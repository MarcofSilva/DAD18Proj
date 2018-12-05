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
        private readonly Object vote_heartbeat_Lock = new object();

        public FollowerState(Server server, int numServers) : base(server, numServers) {
            SetTimer();
        }

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            lock (vote_heartbeat_Lock)
            {
                if (term < _term)
                {
                    //o pedido que recebi e de um lider que ficou para tras
                    //apenas no unperfect
                    Console.WriteLine("Returned false because term of leader is lower");
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
                else
                {
                    electionTimeout.Interval = wait;
                    if (entryPacket.Count == 0)
                    {
                        return new EntryResponse(true, _term, _server.getLogIndex());
                    }
                    _term = term;                           //pode ser != mas visto que se tiver desatualizado e para tras
                    if ((_server.getLogIndex() - 1 + entryPacket.Count) == entryPacket.Entrys[entryPacket.Count - 1].LogIndex)
                    {
                        //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                        //Treasts case of leader changed
                        if (leaderID != _leaderUrl)
                        {
                            _leaderUrl = leaderID;
                            _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                            Console.WriteLine("Follower: Leader is now: " + leaderID);
                        }
                        foreach (Entry entry in entryPacket.Entrys)
                        {
                            _server.addEntrytoLog(entry);
                            //TODO, matilde queres meter a comparacao de strings como gostas? xD
                            if (entry.Type == "write")
                            {
                                _server.writeLeader(entry.Tuple);
                            }
                            else
                            {
                                _server.takeLeader(entry.Tuple);
                            }
                        }
                        return new EntryResponse(true, _term, _server.getLogIndex());
                    }
                    else
                    {
                        Console.WriteLine("Rejected entry because i am not up do date");
                        //manda apenas o log index porque assim o server vai saber quantas entrys no log do follower estao
                        return new EntryResponse(false, _term, _server.getLogIndex());
                    }
                }
            }
        }

        public override bool vote(int term, string candidateID) {
            lock (vote_heartbeat_Lock)
            {
                if (term > _term)
                {
                    _term = term;
                    voted = true;
                    electionTimeout.Interval = wait;
                    return true;
                }
                else if (term == _term)
                {
                    if (!voted)
                    {
                        voted = true;
                        electionTimeout.Interval = wait;
                        return true;
                    }
                }
                return false;
            }
        }
        private void SetTimer() {
            //TODO
            wait = rnd.Next(350, 450);//usually entre 150 300
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
