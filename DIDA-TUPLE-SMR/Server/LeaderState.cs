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
using ExceptionLibrary;

namespace Server {
    public class LeaderState : RaftState{
        private Random rnd = new Random(Guid.NewGuid().GetHashCode());
        private System.Timers.Timer timer;
        private int wait;
        
        public LeaderState(Server server, int numServers) : base(server, numServers) {
            _leaderUrl = server._url;
            SetTimer();
        }

        public override void appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Leader: AppendEntryWrite from: " + leaderID);
                //add this operation to log and then change to follower
                _server.updateState("follower", _term, leaderID);
            }
        }

        public override void appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Follower: AppendEntryTake from: " + leaderID);
                //add this operation to log and then change to follower
                _server.updateState("follower", _term, leaderID);
            }
        }

        public override void heartBeat(int term, string leaderID) {
            if (_term < term) {
                _term = term;
                //Console.WriteLine("Follower: HeartBeat from: " + leaderID);
                //add this operation to log and then change to follower
                _server.updateState("follower", _term, leaderID);
            }
        }

        private void SetTimer() {
            wait = rnd.Next(150, 300);
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

        public override void startClock(int term, string url) {
            if (term > _term) {
                _term = term;
            }
            pulseHeartbeat();
            timer.Start();
            //redundante porque o url recebido e o dele proprio
            _leaderUrl = url;
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
            Console.WriteLine("Leader State pinged ");
        }

        public override bool vote(int term, string candidateID) {
            Console.WriteLine("I WAS IN TERM " + _term + " AND THEY ARE IN TERM " + term);
            if (_term < term) {
                _term = term;
                _server.updateState("follower", _term, candidateID);
                return true;
            }
            //the system doesnt alow it to come here
            return false;
        }
        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            try {
                return _server.readLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
        public override TupleClass take(TupleClass tuple, string url, long nonce) {
            try {
                return _server.takeLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
        public override void write(TupleClass tuple, string url, long nonce) {
            try {
                _server.writeLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
    }
}
    