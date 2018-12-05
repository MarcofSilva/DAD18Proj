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
        private bool timerThreadBlock = false;

        public CandidateState(Server server) : base(server) {
            SetTimer();
        }

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            Console.WriteLine("heartbeat");
            //Console.WriteLine("Candidate: AppendEntryWrite from: " + leaderID);
            if (term < _term) {
                //TODO
                return new EntryResponse(false, _term, _server.getLogIndex());
            }
            else {
                _term = term;
                Console.WriteLine("Leader changed to: " + leaderID);
                if (entryPacket.Count == 0) {
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, leaderID); ;
                    return new EntryResponse(true, _term, _server.getLogIndex());
                }

                if ((_server.getLogIndex() - 1 + entryPacket.Count) != entryPacket.Entrys[entryPacket.Count - 1].LogIndex) {
                    //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                    Console.WriteLine("Candidate -> Follower : appendEntry ");
                    timerThreadBlock = true;
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
                Console.WriteLine("Candidate -> Follower : append Entry");
                timerThreadBlock = true;
                _server.updateState("follower", _term, leaderID);;
                return new EntryResponse(true, _term, _server.getLogIndex());
            }
        }

        public delegate bool voteDelegate(int term, string leaderUrl);

        public void requestVote() {
            if (timerThreadBlock) {
                return;
            }
            _term++;
            Console.WriteLine("Started election in term " + _term);
            int votes = 1;
            if (_server.fd.changed()) {
                _view = _server.fd.getView();
                _numServers = _view.Count();
                foreach (string url in _view) {
                    Console.WriteLine(url);
                }
            }
            WaitHandle[] handles = new WaitHandle[_numServers-1];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers-1];
            try {
                int i = 0;
                foreach (string url in _view) {
                    if (url == _url) {
                        continue;
                    }
                    ServerService remoteObject = (ServerService)_serverRemoteObjects[url];
                    voteDelegate voteDel = new voteDelegate(remoteObject.vote);
                    IAsyncResult ar = voteDel.BeginInvoke(_term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 4000)) {
                    requestVote();
                }
                else {
                    for (i = 0; i < _numServers-1; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        voteDelegate voteDel = (voteDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        bool response = voteDel.EndInvoke(asyncResult);
                        if (response) {
                            votes++;
                        } 
                    }
                    if (votes > (_numServers /2)) {
                        electionTimeout.Stop();
                        timerThreadBlock = true;
                        _server.updateState("leader", _term, _url);
                        return;
                    }
                    else {
                        Console.WriteLine("Finished elections without sucess");
                    }
                }
                wait = rnd.Next(1000, 1200);
                electionTimeout.Interval = wait;
                electionTimeout.Enabled = true;
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
            timerThreadBlock = false;
            requestVote();
            electionTimeout.Start();
        }
        private void SetTimer() {
            //usually entre 150 300
            wait = rnd.Next(200, 400);
            //Console.WriteLine("Election timeout: " + wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = false;
            electionTimeout.Enabled = false;
        }
        public override bool vote(int term, string candidateID) {
            if (term > _term) {
                Console.WriteLine("Candidate -> Follower : vote for " + candidateID);
                Console.WriteLine("He was in term: " + term + " i was in " + _term);
                _term = term;
                timerThreadBlock = true;
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
