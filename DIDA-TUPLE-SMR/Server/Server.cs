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
    public class Server {

        private ReaderWriterLockSlim tupleSpaceLock = new ReaderWriterLockSlim();
        private List<TupleClass> tupleSpace = new List<TupleClass>();

        private CandidateState candidate;
        private FollowerState follower;
        private LeaderState leader;
        private RaftState _state;

        private const int defaultDelay = 0;
        public bool frozen = false;


        //public so state leader can acess it,
        //alternative: send it in constructor of leader
        public string _url = "tcp://localhost:8086/S";
        public string _name = "S";
        private int _port = 8086;
        private TcpChannel channel;
        private ServerService myRemoteObject;

        // public so states can acess it, 
        // alternative: send the map to states or even the map being created in the states
        public Dictionary<string, IServerService> serverRemoteObjects;
        private int _numServers = 0;
        
        private List<Entry> entryLog = new List<Entry>();


        private void selfPrepare(int min_delay, int max_delay) {
            serverRemoteObjects = new Dictionary<string, IServerService>();

            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);

            myRemoteObject = new ServerService(this, min_delay, max_delay);
            RemotingServices.Marshal(myRemoteObject, _name, typeof(ServerService)); //TODO remote object name

            Console.WriteLine("Hello! I'm a Server at port " + _port);

            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                string[] urlSplit = url.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                int portOut;
                Int32.TryParse(urlSplit[2], out portOut);
                //not to connect to himself
                if (portOut != _port) {
                    serverRemoteObjects.Add(url, (ServerService)Activator.GetObject(typeof(ServerService), url));
                }
            }
            _numServers = serverRemoteObjects.Count;

            leader = new LeaderState(this, _numServers);
            candidate = new CandidateState(this, _numServers);
            follower = new FollowerState(this, _numServers);
            _state = follower;
            Console.WriteLine("Finished constructing server "+ _port);
        }

        public Server() {
            selfPrepare(defaultDelay, defaultDelay);
        }

        public Server(string URL, string min_delay, string max_delay) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            _name = urlSplit[3];
            _url = URL;
            int port, imin_delay, imax_delay;
            Int32.TryParse(urlSplit[2], out port);
            Int32.TryParse(min_delay, out imin_delay);
            Int32.TryParse(max_delay, out imax_delay);
            _port = port;
            selfPrepare(imin_delay, imax_delay);
        }


        public void addEntrytoLog(Entry entry) {
            entryLog.Add(entry);
            Console.WriteLine("log size: "+ entryLog.Count +" ,Added this to log: "+entry.ToString());
        }

        //TODOOOO
        public int getLogIndex() {
            return entryLog.Count;
        }

        public EntryResponse heartBeat(int term, string candidateID) {
            return _state.heartBeat(term, candidateID);
        }

        public EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            return _state.appendEntryWrite(writeEntry, term, leaderID);
        }

        public EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            return _state.appendEntryTake(takeEntry, term, leaderID);
        }

        //Enters write mode in the tupleSpaceLock
        public void writeLeader(TupleClass tuple) {
            tupleSpaceLock.EnterWriteLock();
            tupleSpace.Add(tuple);
            tupleSpaceLock.ExitWriteLock();
            Console.WriteLine("Operation: Added" + tuple.ToString() + " tuple space size: " + tupleSpace.Count + "\n");
        }
        //Enters reader mode and if it found same valid tuple it enters write mode to remove it
        public TupleClass takeLeader(TupleClass tuple) {
            tupleSpaceLock.EnterUpgradeableReadLock();  
            try {
                TupleClass res = new TupleClass();
                foreach (TupleClass el in tupleSpace)
                {
                    if (el.Matches(tuple))
                    {
                        tupleSpaceLock.EnterWriteLock();
                        try {
                            res = el;
                            tupleSpace.Remove(el);
                            Console.WriteLine("Operation: Took " + res.ToString() + " tuple space size: " + tupleSpace.Count + "\n");
                            return res;
                        }
                        finally {
                            tupleSpaceLock.ExitWriteLock();
                        }
                    }
                }

                Console.WriteLine("Operation: Took " + res.ToString() + " tuple space size: " + tupleSpace.Count + "\n");
                return res; //no match
            }
            finally
            {
                tupleSpaceLock.ExitUpgradeableReadLock();
            }
        }
        //reader mode of tupleSpaceLock
        public List<TupleClass> readLeader(TupleClass tuple, bool verbose) {
            tupleSpaceLock.EnterReadLock();
            //verbose esta aqui porque no take, utilizamos, e n queremos que faca print de read
            if (verbose) {
                Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");
            }
            List<TupleClass> res = new List<TupleClass>();
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            tupleSpaceLock.ExitReadLock();
            return res;
        }
        public void updateState(string state, int term, string url) {
            if (state == "follower") {
                _state.stopClock();
                Console.WriteLine("I am now a Follower");
                _state = follower;
            }
            else if (state == "candidate") {
                _state.stopClock();
                Console.WriteLine("I am now a Candidate");
                _state = candidate;
            }
            else if(state == "leader") {
                _state.stopClock();
                Console.WriteLine("I am now a Leader");
                _state = leader;
            }
            _state.startClock(term, url);
        }
        public void write(TupleClass tuple, string clientUrl, long nonce) {
            try {
                _state.write(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
        public List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            try {
                return _state.read(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
        public TupleClass take(TupleClass tuple, string clientUrl, long nonce) {
            try {
                return _state.take(tuple, clientUrl, nonce);
            }
            catch (ElectionException e) {
                throw e;
            }
        }
        public bool vote(int term, string candidateID) {
            return _state.vote(term, candidateID);
        }
        public void Freeze() {
            frozen = true;
        }
        public void checkFrozen() {
            if (frozen) {
                Console.WriteLine("Cant do anything, im frozen");
                lock (this) {
                    while (frozen) {
                        Console.WriteLine("Waiting...");
                        Monitor.Wait(this);
                    }
                }
            }
        }
        public void Unfreeze() {
            Console.WriteLine("Unfreezing...");
            lock (this) {
                Monitor.PulseAll(this);
            }
            frozen = false;
        }
        static void Main(string[] args) {
            Server server;
            if (args.Length == 0) {
                server = new Server();
            }
            else {
                server = new Server(args[0], args[1], args[2]);
            }
            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }
    }
}
