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
        private Random rnd = new Random();
        private System.Timers.Timer electionTimeout;
        private int wait;
        private bool voted = false;

        public FollowerState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }

        public override void appendEntry(int term, string senderID) {
            throw new NotImplementedException();
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
        
        public override void heartBeat(int term, string candidateID) {
            electionTimeout.Interval = wait;
            if (term > _term) {
                //_term = term;
                //here to prevent heartbeats from past term
            }
            if (candidateID != _leaderUrl) {
                //TODO quando vem do candidate state o leader url e o leader remote n tao assigned
                Console.WriteLine("Leader changed to: " + candidateID);
                _leaderUrl = candidateID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("heartbeat candidate state");
            }
        }

        private void SetTimer() {
            //TODO
            wait = rnd.Next(300, 500);//usually entre 150 300
            Console.WriteLine("follower will wait for: " +wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
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

        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            //Console.WriteLine("READ IN FOLLOWER CALLED");
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
            Console.WriteLine("WRITE IN FOLLOWER CALLED");
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
            Console.WriteLine("WRITE IN FOLLOWER CALLED");
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
