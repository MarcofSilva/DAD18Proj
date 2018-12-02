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
    public class FollowerState : RaftState {
        private IServerService _leaderRemote;
        private Random rnd = new Random();
        private System.Timers.Timer electionTimeout;
        private int wait;
        private bool voted = false;

        public FollowerState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override bool vote(int term, string candidateID) {
            if (term > _term) {
                _term = term;
                voted = true;
                return true;
            }
            if (!voted) {
                voted = true;
                return true;
            }
            return false;
        }
        /*
        public override void electLeader(int term, string leaderUrl) {
            _term = term;
            _leaderUrl = leaderUrl;
            _leaderRemote = _serverRemoteObjects[_leaderUrl];
            Console.WriteLine("Follower: " + _server._url);
            Console.WriteLine("My leader is: " + leaderUrl);
            SetTimer();
        }*/

        public override void heartBeat(int term, string candidateID) {
            electionTimeout.Interval = wait;
            if (term > _term) {
                //here to prevent heartbeats from past term
            }
            if (candidateID != _leaderUrl) {
                Console.WriteLine("Leader changed to: " + candidateID);
                _leaderUrl = candidateID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("heartbeat candidate state");
                _server.updateState("follower");
            }
        }

        private void SetTimer() {
            wait = rnd.Next(300, 500);//usually entre 150 300
            Console.WriteLine("follower will wait for: " +wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;            
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            //tornar candidato
            Console.WriteLine("NAO RECEBI HEARTBEAT");
            _server.updateState("candidate");
        }

        public override void ping() {
            Console.WriteLine("Follower State pinged");
        }

        public override void stopClock() {
            electionTimeout.Stop();
        }

        public override void startClock() {
            electionTimeout.Start();
        }

        public delegate List<TupleClass> readDelegate(TupleClass tuple, string url, long nonce);

        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            Console.WriteLine("READ IN FOLLOWER CALLED");

            //TODO resolver estas cenas de criar lista com apenas 1 elemento
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                readDelegate readDel = new readDelegate(_leaderRemote.read);
                asyncResults[0] = readDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple, clientUrl, nonce);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    List<TupleClass> res = readDel.EndInvoke(asyncResult);
                    Console.WriteLine("----->DEBUG_FollowerState: " + res[0].ToString());
                    return res;
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate List<TupleClass> takeDelegate(TupleClass tuple, string url, long nonce);

        public override List<TupleClass> take(TupleClass tuple, string clientUrl, long nonce) {
            Console.WriteLine("TAKE IN FOLLOWER CALLED");

            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                takeDelegate takeDel = new takeDelegate(_leaderRemote.take);
                asyncResults[0] = takeDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple, clientUrl, nonce);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    takeDel = (takeDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    return takeDel.EndInvoke(asyncResult);
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate void writeDelegate(TupleClass tuple, string clientUrl, long nonce);

        public override void write(TupleClass tuple, string clientUrl, long nonce) {
            Console.WriteLine("WRITE IN FOLLOWER CALLED");
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                writeDelegate takeDel = new writeDelegate(_leaderRemote.write);
                asyncResults[0] = takeDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    read(tuple, clientUrl, nonce);
                }
                else {
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
    }
}
