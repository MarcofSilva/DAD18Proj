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

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            //Console.WriteLine("Candidate: AppendEntryWrite from: " + leaderID);
            if (term < _term) {
                //TODO
                return new EntryResponse(false, _term, _server.getLogIndex());
            }
            else {
                _term = term;
                Console.WriteLine("Leader changed to: " + leaderID);
                if ((_server.getLogIndex() - 1 + entryPacket.Count) != entryPacket.Entrys[entryPacket.Count - 1].LogIndex) {
                    //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                    _server.updateState("follower", _term, leaderID);
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
                foreach (Entry entry in entryPacket.Entrys) {
                    _server.addEntrytoLog(entry);
                    //TODO, matilde queres meter a comparacao de strings como gostas? xD
                    if (entry.Type == "write") {
                        _server.writeLeader(entry.Tuple);
                    }
                    else {
                        _server.takeLeader(entry.Tuple);
                    }
                }
                _server.updateState("follower", _term, leaderID);;

                return new EntryResponse(true, _term, _server.getLogIndex());
            }
        }

        public delegate bool voteDelegate(int term, string leaderUrl);

        public void requestVote() {
            _term++;
            Console.WriteLine("NEW ELECTION BITCHES, MY TERM IS " + _term);
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
                        //Console.WriteLine(i + " voted " + response);
                        if (response) {
                            votes++;
                        }
                    }
                    if (votes > ((_numServers + 1)/2) ) {
                        electionTimeout.Stop();
                        _server.updateState("leader", _term, _url);
                    }
                    else {
                        Console.WriteLine("Finished elections without sucess");
                    }
                }
                wait = rnd.Next(350, 450);
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
                Console.WriteLine("I CHANGED WHEN I WAS ASKED A VOTE WITH TERM " + term);
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
