using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using ClassLibrary;
using System.Runtime.Remoting.Messaging;
using System.Timers;

namespace Server {
    public class LeaderState : RaftState{
        private Random rnd = new Random();
        private System.Timers.Timer timer;
        private int wait;
        
        public LeaderState(Server server, int numServers) : base(server, numServers) {
            _leaderUrl = server._url;
            SetTimer();
        }

        public override void heartBeat(int term, string candidateID) {
            if (_term < term) {
                _server.updateState("follower");
            }
        }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            /* Para as 3 operaçoes iguais
        Each client request contains a command to be executed by the replicated state machines. The leader appends the command 
        to its log as a new entry, then issues AppendEntries RPCs in parallel to each of the other servers to replicate the entry.
        When the entry has been safely replicated (as described below), the leader applies the entry to its state machine and 
        returns the result of thatexecution to the client
        If followers crash or run slowly, or if network packets are lost, the leader retries AppendEntries RPCs indefinitely 
        (even after it has responded to the client) until all followers eventually store all log entries.        The term numbers in log entries are used to detect inconsistencies between logs        Each log entry stores a state machine command along with the term number when the entry was received by the leader
        Each log entry also has an integer index identifying its position in the log

        The leader keeps track of the highest index it knows to be committed, and it includes that index in future
        AppendEntries RPCs (including heartbeats) so that the other servers eventually find out. 
        Once a follower learns  that a log entry is committed, it applies the entry to its local state machine (in log order).        • If two entries in different logs have the same index and term, then they store the same command.
        • If two entries in different logs have the same index and term, then the logs are identical in all preceding entries.
            */

            return _server.readLeader(tuple);
        }
        public override List<TupleClass> take(TupleClass tuple, string url, long nonce) {
            return _server.takeLeader(tuple);
        }
        public override void write(TupleClass tuple, string url, long nonce) {
            _server.writeLeader(tuple);
        }

        private void SetTimer() {
            wait = rnd.Next(150, 250);
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Stop();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
        }

        public override void stopClock() {
            timer.Stop();
        }

        public override void startClock() {
            pulseHeartbeat();
            timer.Start();
        }

        public delegate string heartBeatDelegate(int term, string candidateID);

        private void pulseHeartbeat() {
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    heartBeatDelegate heartBeatDel = new heartBeatDelegate(remoteObject.heartBeat);
                    IAsyncResult ar = heartBeatDel.BeginInvoke(_term,_url,null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {
                    pulseHeartbeat();
                }
                else {
                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        string response = heartBeatDel.EndInvoke(asyncResult);
                        //Console.WriteLine(response);
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        
        public override void ping() {
            Console.WriteLine("Leader State pinged");
        }

        public override bool vote(int term, string candidateID) {
            throw new NotImplementedException();
        }
    }
}
    