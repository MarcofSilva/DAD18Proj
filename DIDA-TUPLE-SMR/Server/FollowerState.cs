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

        //private Dictionary<string, bool> voteMap = new Dictionary<string, bool>();

        private bool timerThreadBlock = false;
        private bool leaveState = false;

        public FollowerState(Server server, int term) : base(server, term) {
            SetTimer();
            Console.WriteLine("Created follower");
        }

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            lock (vote_heartbeat_Lock)
            {
                //Console.WriteLine("heartbeat");
                if (term < _term)
                {
                    //o pedido que recebi e de um lider que ficou para tras
                    //apenas no unperfect
                    Console.WriteLine("Returned false because term of leader is lower");
                    return new EntryResponse(false, _term, _server.getLogIndex());
                }
                else
                {
                    try
                    {
                        if (!electionTimeout.Enabled) //caso em que timer acabou durante o processamento de um heartbeat
                        {
                            timerThreadBlock = true;
                        }
                        else
                        {
                            electionTimeout.Stop();
                        }
                        electionTimeout.Interval = setWait();

                        if (entryPacket.Count == 0)
                        {
                            if (leaderID != _leaderUrl)
                            {

                                _leaderUrl = leaderID;
                                _leaderRemote = _server.serverRemoteObjects[_leaderUrl];
                                //Console.WriteLine("Follower: Leader is now: " + leaderID);
                            }
                            return new EntryResponse(true, _term, _server.getLogIndex());
                        }
                        Console.WriteLine("UPDATE TERM IN APPEND ENTRY");
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
                            return new EntryResponse(true, _term, _server.getLogIndex());
                        }
                        else
                        {
                            Console.WriteLine("Rejected entry because i am not up do date");
                            //manda apenas o log index porque assim o server vai saber quantas entrys no log do follower estao
                            return new EntryResponse(false, _term, _server.getLogIndex());
                        }
                    }
                    finally
                    {
                        electionTimeout.Start();
                    }
                }
            }
        }

        public override bool vote(int term, string candidateID) {
            lock (vote_heartbeat_Lock) {
                try
                {
                    if (term > _term)
                    {
                        if (!electionTimeout.Enabled) //caso em que timer acabou durante o processamento de um heartbeat
                        {
                            timerThreadBlock = true;
                        }
                        else
                        {
                            electionTimeout.Stop();
                        }
                        electionTimeout.Interval = setWait();

                        Console.WriteLine("UPDATE TERM IN VOTE from:" + _term + " to " + term);
                        _term = term;
                        voted = true;
                        return true;
                    }
                    else if (term == _term)
                    {
                        if (!voted)
                        {
                            if (!electionTimeout.Enabled) //caso em que timer acabou durante o processamento de um heartbeat
                            {
                                timerThreadBlock = true;
                            }
                            else
                            {
                                electionTimeout.Stop();
                            }
                            electionTimeout.Interval = setWait();

                            voted = true;
                            return true;
                        }
                        Console.WriteLine("já votei noutro");
                    }
                }
                finally
                {
                    electionTimeout.Start();
                }
                return false;
            }
        }

        private int setWait()
        {
            return wait = rnd.Next(300, 900);//usually entre 150 300
        }

        private void SetTimer() {
            setWait();
            Console.WriteLine(wait);
            electionTimeout = new System.Timers.Timer(wait);
            electionTimeout.Elapsed += OnTimedEvent;
            electionTimeout.AutoReset = false;
            electionTimeout.Enabled = true;
        }
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            lock (vote_heartbeat_Lock)
            {
                //Console.WriteLine("timeevent ->>" + timerThreadBlock);
                if (!timerThreadBlock)
                {
                    Console.WriteLine("Follower -> candidate : ontimedevent -- Thread:" + Thread.CurrentThread.ManagedThreadId);
                    // TODO variavel para controlar negaçao de heartbeat ou vote que chegue dps do timer acabar
                    _server.updateState("candidate", _term, ""); //sends empty string because there is no leader
                    _server = null;
                    electionTimeout.Dispose();
                    leaveState = true;
                }
                else
                {
                    timerThreadBlock = false;
                }
            }
        }
        public override void ping() {
            Console.WriteLine("Follower State pinged");
        }
        public override void stopClock() {
            electionTimeout.Stop();
            electionTimeout.Dispose();
        }
        public override void startClock(int term, string url) {
            //Console.WriteLine("Started clock on Follower");
            //quando vem de candidato
            Console.WriteLine("UPDATE TERM IN START");
            if (term > _term) {
                _term = term;
            }
            SetTimer();
            //electionTimeout.Interval = wait;
            //electionTimeout.Enabled = true;
            _leaderUrl = url;
            _leaderRemote = _serverRemoteObjects[url];
            
        }
        public override TupleClass read(TupleClass tuple, string clientUrl, long nonce) {
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
