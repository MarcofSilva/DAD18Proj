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

        public override EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            Console.WriteLine("Follower: appendEntryWrite from: " + leaderID);
            electionTimeout.Interval = wait;
            //Considers requests from old entry
            if (term > _term ) {
                //posso atualizar primeiro, porque apesar de na response nao mostrar que estava atrasado, devido ao logindex da para ver 
                _term = term;
                return new EntryResponse(false, _term, _server.getLogIndex()); ;
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);

                // verfifica se a posicao para onde vai o a entry e menor que onde era suposto ela ficar
                // nao verificamos se e maior, porque e impossivel
                // verificamos so aqui, porque so e impossivel isto acontecer se o lider mudou
                // se o lider nunca mudou, o follower acompanhou sempre
                //TODO acham que isto e verdade? ^
                if (_server.getLogIndex() < writeEntry.LogIndex) {
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
            }
            _server.addEntrytoLog(writeEntry);
            _server.writeLeader(writeEntry.Tuple);
            return new EntryResponse(true, _term, _server.getLogIndex()-1);
        }

        public override EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            Console.WriteLine("Follower: appendEntryTake from: " + leaderID);
            electionTimeout.Interval = wait;
            //Considers requests from old entry
            if (term > _term) {
                _term = term;
                return new EntryResponse(false, _term, _server.getLogIndex()); ;
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);

                // verfifica se a posicao para onde vai o a entry e menor que onde era suposto ela ficar
                // nao verificamos se e maior, porque e impossivel
                // verificamos so aqui, porque so e impossivel isto acontecer se o lider mudou
                // se o lider nunca mudou, o follower acompanhou sempre
                //TODO acham que isto e verdade? ^
                if ( _server.getLogIndex() < takeEntry.LogIndex) {
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
            }
            _server.addEntrytoLog(takeEntry);
            _server.takeLeader(takeEntry.Tuple);
            return new EntryResponse(true, _term, _server.getLogIndex()-1);
        }

        public override EntryResponse heartBeat(int term, string leaderID) {
            //Considers requests from old entry
            electionTimeout.Interval = wait;
            if (term > _term) {
                _term = term;
                return new EntryResponse(false, _term, _server.getLogIndex());
            }
            //Treasts case of leader changed
            if (leaderID != _leaderUrl) {
                _leaderUrl = leaderID;
                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                Console.WriteLine("Follower: Leader is now: " + leaderID);
            }
            //ter em atencao se do outro lado no heartbeat response verificamos isto
            return new EntryResponse(true, _term, _server.getLogIndex());
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
                electionTimeout.Interval = wait;
                return true;
            }
            electionTimeout.Interval = wait;
            return false;
        }
        private void SetTimer() {
            //TODO
            wait = rnd.Next(400, 600);//usually entre 150 300
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = true;
            electionTimeout.Enabled = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            Console.WriteLine("I CHANGED BECAUSE OF ON TIMED EVENT ON FOLLOWER");
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
                Console.WriteLine("Read in follower");
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
                Console.WriteLine("Write called in follower");
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
                Console.WriteLine("Take in follower");
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
