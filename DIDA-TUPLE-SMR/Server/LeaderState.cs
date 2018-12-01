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
            electLeader(_term, _leaderUrl);
            SetTimer();
            Console.WriteLine("LEADER STATE CONSTRUCTED");
        }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override void requestVote(int term, string candidateID) {
            throw new NotImplementedException();
        }

        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            return _server.readLeader(tuple);
        }

        public override List<TupleClass> take(TupleClass tuple, string url, long nonce) {
            return _server.takeLeader(tuple);
        }

        public override void write(TupleClass tuple, string url, long nonce) {
            Console.WriteLine("----->DEBUG_State: Received Write Request");
            _server.writeLeader(tuple);
        }

        private void SetTimer() {
            wait = rnd.Next(2000, 2200);
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
        }

        public void terminateClock() {
            timer.Stop();
            timer.Dispose();
        }

        public delegate string heartBeatDelegate();

        private void pulseHeartbeat() {
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    heartBeatDelegate heartBeatDel = new heartBeatDelegate(remoteObject.heartBeat);
                    IAsyncResult ar = heartBeatDel.BeginInvoke(null, null);
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
                        Console.WriteLine(response);
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate void electLeaderDelegate(int term, string leaderUrl);

        public override void electLeader(int term, string leaderUrl) {
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers];
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    electLeaderDelegate electLeaderDel = new electLeaderDelegate(remoteObject.electLeader);
                    IAsyncResult ar = electLeaderDel.BeginInvoke(term, leaderUrl, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {
                    pulseHeartbeat();
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
    }
}
    