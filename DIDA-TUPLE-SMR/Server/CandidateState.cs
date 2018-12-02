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
    public class CandidateState : RaftState {
        private System.Timers.Timer electionTimeout;
        private int wait;
        private Random rnd = new Random();

        public CandidateState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }
        public override void stopClock() {
            electionTimeout.Stop();
        }

        public override void startClock() {
            requestVote();
            electionTimeout.Start();
        }
        private void SetTimer() {
            //usually entre 150 300
            wait = rnd.Next(300,500);
            Console.WriteLine("Election timeout: " + wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;
            electionTimeout.Stop();

        }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override bool vote(int term, string candidateID) {
            if (term > _term) {
                _term = term;
                _server.updateState("follower");
                return true;
            }
            return false;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            requestVote();
        }

        public delegate bool voteDelegate(int term, string leaderUrl);

        public void requestVote() {
            _term++;
            //Console.WriteLine("REQUEST VOTE in term " + _term);
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                int i = 0;
                //votes start at one because he votes for himself
                int votes = 1;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    voteDelegate voteDel = new voteDelegate(remoteObject.vote);
                    IAsyncResult ar = voteDel.BeginInvoke(_term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {
                    requestVote();
                }
                else {
                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        voteDelegate voteDel = (voteDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        bool response = voteDel.EndInvoke(asyncResult);
                        if (response) {
                            votes++;
                        }
                    }
                    if (votes > (_numServers/2) ) {
                        _server.updateState("leader");
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        //TODO lancar excepcao e apanha no api do cliente
        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }

        public override List<TupleClass> take(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }

        public override void write(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }
        /*
        public override void electLeader(int term, string leaderUrl) {
            throw new NotImplementedException();
        }*/
        public override void ping() {
            Console.WriteLine("Candidate State pinged");
        }

        public override void heartBeat(int term, string candidateID) {
            Console.WriteLine("I am a candidate, received heart beat");
            if(term < _term) {
                //TODO
                //here to prevent heartbeats from past term
            }
            else {
                _term = term;
                Console.WriteLine("Leader changed to: " + candidateID);

                //TODO nao deveria ser aqui
                _leaderUrl = candidateID;
                _server.updateState("follower");
            }
            
        }
    }
}
