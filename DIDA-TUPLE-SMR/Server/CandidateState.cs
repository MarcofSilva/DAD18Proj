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
        private System.Timers.Timer pulseVote;
        private int wait;
        private Random rnd = new Random(Guid.NewGuid().GetHashCode());
        private bool timerThreadBlock = false;
        private readonly Object vote_heartbeat_Lock = new object();
        private Dictionary<string, bool> votemap = new Dictionary<string, bool>();
        
        private int votes = 1;
        public CandidateState(Server server, int term) : base(server, term) {
            SetTimer();
            _term++;
            foreach(string url in _server.fd.getViewNormal()) {
                if (url == _url) {
                    continue;
                }
                votemap.Add(url, false);
            }
        }

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            lock (vote_heartbeat_Lock)
            {
                Console.WriteLine("heartbeat");
                //Console.WriteLine("Candidate: AppendEntryWrite from: " + leaderID);
                if (term < _term)
                {
                    //TODO
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
                else
                {
                    _term = term;
                    Console.WriteLine("Leader changed to: " + leaderID);
                    if (entryPacket.Count == 0)
                    {
                        timerThreadBlock = true;
                        stopClock();
                        _server.updateState("follower", _term, leaderID);
                        _server = null;
                        return new EntryResponse(true, _term, _server.getLogIndex());
                    }

                    if ((_server.getLogIndex() - 1 + entryPacket.Count) != entryPacket.Entrys[entryPacket.Count - 1].LogIndex)
                    {
                        //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                        Console.WriteLine("Candidate -> Follower : appendEntry ");
                        timerThreadBlock = true;
                        stopClock();
                        _server.updateState("follower", _term, leaderID);
                        _server = null;
                        return new EntryResponse(false, _term, _server.getLogIndex());
                    }
                    foreach (Entry entry in entryPacket.Entrys)
                    {
                        if (entry.Type == "write")
                        {
                            _server.addEntrytoLog(entry);
                            _server.writeLeader(entry.Tuple);
                        }
                        else
                        {
                            _server.takeLeader(entry.Tuple, entry.Term);
                        }
                    }
                    Console.WriteLine("Candidate -> Follower : append Entry");
                    timerThreadBlock = true;
                    stopClock();
                    _server.updateState("follower", _term, leaderID);
                    _server = null;
                    return new EntryResponse(true, _term, _server.getLogIndex());
                }
            }
        }

        public delegate bool voteDelegate(int term, string leaderUrl);

        public void requestVote() {
            //Console.WriteLine("request_vote --t " + Thread.CurrentThread.ManagedThreadId);
            if (timerThreadBlock) {
                return;
            }
            //Console.WriteLine("pulsed vote");

            if (_server.fd.changed()) {
                _view = _server.fd.getView();
                _numServers = _view.Count();
                Console.WriteLine("ATUALIZOU A VIEW");
                foreach(string url in _view) {
                    if (!votemap.ContainsKey(url)) {
                        votemap.Add(url, false);
                    }
                }
                foreach(KeyValuePair<string, bool> entry in votemap) {
                    if (!_view.Contains(entry.Key)) {
                        votemap.Remove(entry.Key);
                    }
                }
            }

            Console.WriteLine("after view change THERE ARE SERVERS: " + _numServers);

            int howmany = 0;
            foreach (KeyValuePair<string, bool> entry in votemap) {
                if (!entry.Value) {
                    howmany++;
                }
            }
            Console.WriteLine("requesting vote to howmany: " + howmany);

            WaitHandle[] handles = new WaitHandle[howmany];
            IAsyncResult[] asyncResults = new IAsyncResult[howmany];
            string[] requestId = new string[howmany];
            try {
                int i = 0;
                foreach (KeyValuePair<string, bool> entry in votemap) {
                    if (entry.Value) {
                        continue;
                    }
                    ServerService remoteObject = (ServerService)_serverRemoteObjects[entry.Key];
                    voteDelegate voteDel = new voteDelegate(remoteObject.vote);
                    IAsyncResult ar = voteDel.BeginInvoke(_term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    requestId[i] = entry.Key;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 800)) {//TODO
                    Console.WriteLine("candidate timeout waiting for votes");

                    requestVote();
                }
                else {
                    for (i = 0; i < howmany ; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        voteDelegate voteDel = (voteDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        bool response = voteDel.EndInvoke(asyncResult);
                        votemap.Remove(requestId[i]);
                        votemap.Add(requestId[i],true);
                        if (response) {
                            votes++;
                        } 
                    }
                    if (votes > (_numServers /2)) {
                        stopClock();
                        timerThreadBlock = true;
                        _server.updateState("leader", _term, _url);
                        _server = null;
                        Console.WriteLine("elected in term" + _term);
                        return;
                    }
                    else {
                        Console.WriteLine("Finished elections without sucess");
                        pulseVote.Stop();
                        pulseVote.Dispose();
                        SetVoteTimer();
                    }
                }

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
            SetTimer();
        }

        private int setWait()
        {
            return wait = rnd.Next(500, 800);
        }

        private void SetTimer() {
            setWait();
            //Console.WriteLine("Election timeout: " + wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = false;
            electionTimeout.Enabled = true;

            pulseVote = new System.Timers.Timer(100);
            pulseVote.Elapsed += pulseVoteEvent;
            pulseVote.AutoReset = false;
            pulseVote.Enabled = true;
        }

        private void SetVoteTimer()
        {
            pulseVote = new System.Timers.Timer(100);
            pulseVote.Elapsed += pulseVoteEvent;
            pulseVote.AutoReset = false;
            pulseVote.Enabled = true;
        }

        public override void playClock() {
            electionTimeout.Start();
            pulseVote.Start();
        }

        public override void pauseClock() {
            electionTimeout.Stop();
            pulseVote.Stop();
        }

        private void pulseVoteEvent(Object source, ElapsedEventArgs e) {
            requestVote();
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            Console.WriteLine("INCREMNETOU NOVA ELEICAO");
            _term++;
            votemap = new Dictionary<string, bool>();
            foreach (string url in _server.fd.getViewNormal()) {
                if(url == _url) {
                    continue;
                }
                votemap.Add(url, false);
            }
            votes = 1;
            lock (vote_heartbeat_Lock) {
                Console.WriteLine("timeevent in Candidate->>" + timerThreadBlock);
                if (!timerThreadBlock) {
                    requestVote();
                }
                else {
                    timerThreadBlock = false;
                }
            }
        }

        public override bool vote(int term, string candidateID) {
            lock (vote_heartbeat_Lock)
            {
                if (term > _term)
                {
                    timerThreadBlock = true;
                    stopClock();
                    Console.WriteLine("Candidate -> Follower : vote for " + candidateID);
                    Console.WriteLine("He was in term: " + term + " i was in " + _term);
                    _term = term;
                    stopClock();
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, candidateID);
                    _server = null;
                    electionTimeout.Dispose();
                    return true;
                }
            }
            return false;
        }
        
        public override void ping() {
            Console.WriteLine("Candidate State pinged");
        }
        public override void stopClock() {
            Console.WriteLine("stoped clocks");
            electionTimeout.Stop();
            electionTimeout.Dispose();
            pulseVote.Stop();
            pulseVote.Dispose();
        }
        public override TupleClass read(TupleClass tuple, string clientUrl, long nonce) {
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
