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
    public class CandidateState : RaftState {
        private System.Timers.Timer electionTimeout;
        private int wait;
        private Random rnd = new Random(Guid.NewGuid().GetHashCode());

        public CandidateState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }

        public override EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            //Console.WriteLine("Candidate: AppendEntryWrite from: " + leaderID);
            if (term < _term) {
                //TODO
                return new EntryResponse(false, _term, 0);
                //TODO se o appendEntryWrite e antigo, nao responder
                //o paper diz para simplesmente continuar no candidate state
            }
            else {
                _term = term;
                Console.WriteLine("Leader changed to: " + leaderID);
                _server.addEntrytoLog(writeEntry);
                _server.updateState("follower", _term, leaderID);

                //TODO visto que muda o estado e depois retorna o entry n sei se funciona
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
        }

        public override EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            //Console.WriteLine("Candidate: AppendEntryTake from: " + leaderID);
            if (term < _term) {
                return new EntryResponse(false, _term, 0);
                //TODO se o appendEntryTake e antigo, nao responder
                //o paper diz para simplesmente continuar no candidate state
            }
            else {
                _term = term;
                Console.WriteLine("Leader changed to: " + leaderID);
                _server.addEntrytoLog(takeEntry);
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
        }

        public override EntryResponse heartBeat(int term, string leaderID) {
            Console.WriteLine("heartbeat");
            if (term < _term) {
                return new EntryResponse(false, _term, 0);
                //TODO se o heartbeat e antigo, nao responder
                //o paper diz para simplesmente continuar no candidate state
            }
            else {
                Console.WriteLine("heartbeat that counts");
                _term = term;
                Console.WriteLine("Leader changed to: " + leaderID);
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
        }

        public delegate bool voteDelegate(int term, string leaderUrl);

        public void requestVote() {
            _term++;
            int votes = 1;
            //randomize the election timeout each iteration
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers];
            try {
                int i = 0;
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
                    if (votes > ((_numServers+1)/2) ) {
                        electionTimeout.Stop();
                        _server.updateState("leader", _term, _url);
                    }
                }
                wait = rnd.Next(350, 500);
                electionTimeout.Interval = wait;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public override void startClock(int term, string url) {
            if (term > _term) {
                _term = term;
            }
            requestVote();
            electionTimeout.Start();
        }
        private void SetTimer() {
            //usually entre 150 300
            wait = rnd.Next(500, 700);
            //Console.WriteLine("Election timeout: " + wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;
            electionTimeout.Stop();
        }
        public override bool vote(int term, string candidateID) {
            if (term > _term) {
                _term = term;
                _server.updateState("follower", _term, candidateID);
                return true;
            }
            return false;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            requestVote();
        }
        public override void ping() {
            Console.WriteLine("Candidate State pinged");
        }
        public override void stopClock() {
            electionTimeout.Stop();
        }
        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            throw new ElectionException("Election going on, try later");
        }
        public override TupleClass take(TupleClass tuple, string clientUrl, long nonce) {
            throw new ElectionException("Election going on, try later");
        }
        public override void write(TupleClass tuple, string clientUrl, long nonce) {
            throw new ElectionException("Election going on, try later");
        }
    }
}
