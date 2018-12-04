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
    public class FollowerState : RaftState {
        private IServerService _leaderRemote;
        private Random rnd = new Random(Guid.NewGuid().GetHashCode());
        private System.Timers.Timer electionTimeout;
        private int wait;
        private bool voted = false;

        public FollowerState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }

        public override void appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            Console.WriteLine("Follower: appendEntryWrite from: " + leaderID);
            //Considers requests from old entry
            if (term > _term) {
                return;
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                electionTimeout.Interval = wait;
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);
            }
            //add entry to log
        }

        public override void appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            Console.WriteLine("Follower: appendEntryTake from: " + leaderID);
            //Considers requests from old entry
            if (term > _term) {
                return;
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                electionTimeout.Interval = wait;
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);
            }
            //add entry to log
        }
        public override void heartBeat(int term, string leaderID) {
            //Console.WriteLine("Follower: heartBeat from: " + leaderID);
            //Considers requests from old entry
            if (term > _term) {
                return;
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);
            }
            electionTimeout.Interval = wait;
            //add entry to log
        }

        public override bool vote(int term, string candidateID) {
            if (term > _term) {
                _term = term;
                voted = true;
                electionTimeout.Interval = wait;
                return true;
            }
            if (!voted) {
                voted = true;
                return true;
            }
            return false;
        }
        private void SetTimer() {
            //TODO
            wait = rnd.Next(350, 500);//usually entre 150 300
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            _server.updateState("candidate", _term, ""); //sends empty string because there is no leader
        }
        public override void ping() {
            Console.WriteLine("Follower State pinged");
        }
        public override void stopClock() {
            electionTimeout.Stop();
        }
        public override void startClock(int term, string url) {
            //quando vem de candidato
            if (term > _term) {
                _term = term;
            }
            _leaderUrl = url;
            _leaderRemote = _serverRemoteObjects[url];
            electionTimeout.Start();
        }
        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            try {
                return _leaderRemote.read(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public override void write(TupleClass tuple, string clientUrl, long nonce) {
            try {
                _leaderRemote.write(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public override TupleClass take(TupleClass tuple, string clientUrl, long nonce) {
            try {
                return _leaderRemote.take(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
    }
}
