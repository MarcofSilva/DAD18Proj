﻿using RemoteServicesLibrary;
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
        public Dictionary<string, int> matchIndexMap;
        private int _numServers = 0;
        
        public List<Entry> entryLog = new List<Entry>();
        public FailureDetector fd;


        private void selfPrepare(int min_delay, int max_delay) {
            serverRemoteObjects = new Dictionary<string, IServerService>();
            matchIndexMap = new Dictionary<string, int>();

            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);

            myRemoteObject = new ServerService(this, min_delay, max_delay);
            RemotingServices.Marshal(myRemoteObject, _name, typeof(ServerService)); //TODO remote object name
            fd = new FailureDetector();
            Console.WriteLine("Hello! I'm a Server at port " + _port);

            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                string[] urlSplit = url.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                int portOut;
                Int32.TryParse(urlSplit[2], out portOut);
                //not to connect to himself
                if (portOut != _port) {
                    serverRemoteObjects.Add(url, (ServerService)Activator.GetObject(typeof(ServerService), url));
                    matchIndexMap.Add(url, 0);
                }
                
            }
            _numServers = serverRemoteObjects.Count;

            List<string> view = fd.getView();
            while (view == null || view.Count == 0) {
                Thread.Sleep(100);
                view = fd.getView();
            }
            _state = new FollowerState(this, 0); ;
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
        }

        public int getLogIndex() {
            return entryLog.Count;
        }

        public EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID) {
            return _state.appendEntry(entryPacket, term, leaderID);
        }

        //Enters write mode in the tupleSpaceLock
        public void writeLeader(TupleClass tuple) {
            tupleSpaceLock.EnterWriteLock();
            tupleSpace.Add(tuple);
            Console.WriteLine("Operation: Added " + tuple.ToString() + "\n");
            tupleSpaceLock.ExitWriteLock();
        }
        //Enters reader mode and if it found same valid tuple it enters write mode to remove it
        public TupleClass takeLeader(TupleClass tuple, int term) {
            tupleSpaceLock.EnterUpgradeableReadLock();  
            try {
                TupleClass res = new TupleClass();
                foreach (TupleClass el in tupleSpace)
                {
                    if (el.Matches(tuple))
                    {
                        tupleSpaceLock.EnterWriteLock();
                        try {
                            TakeEntry entry = new TakeEntry(tuple, term, getLogIndex(), "take");
                            addEntrytoLog(entry);
                            res = el;
                            tupleSpace.Remove(el);
                            Console.WriteLine("Operation: Took " + res.ToString() + "\n");
                            return res;
                        }
                        finally {
                            tupleSpaceLock.ExitWriteLock();
                        }
                    }
                }

                Console.WriteLine("Operation: Took " + res.ToString() + "\n");
                return res; //no match
            }
            finally
            {
                tupleSpaceLock.ExitUpgradeableReadLock();
            }
        }
        //reader mode of tupleSpaceLock
        public TupleClass readLeader(TupleClass tuple, bool verbose) {
            tupleSpaceLock.EnterReadLock();
            //verbose esta aqui porque no take, utilizamos, e n queremos que faca print de read
            if (verbose) {
                Console.WriteLine("Operation: Read " + tuple.ToString() + "\n");
            }
            TupleClass res = new TupleClass();
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res = el;
                    break;
                }
            }
            tupleSpaceLock.ExitReadLock();
            return res;
        }

        public void updateState(string state, int term, string url) {
            if (state == "follower") {
                _state.stopClock();
                Console.WriteLine("I am now a Follower");
                _state = new FollowerState(this, term);
            }
            else if (state == "candidate") {
                _state.stopClock();
                Console.WriteLine("I am now a Candidate");
                _state = new CandidateState(this, term);
            }
            else if(state == "leader") {
                _state.stopClock();
                Console.WriteLine("I am now a Leader");
                _state = new LeaderState(this, term);
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
        public TupleClass read(TupleClass tuple, string clientUrl, long nonce) {
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
            _state.pauseClock();
        }
        public void checkFrozen() {
            if (frozen) {
                Console.WriteLine("Can't do anything, I'm frozen");
                lock (this) {
                    while (frozen) {
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

        public int ping() { 
            return 1;
        }

        public List<string> viewRequest() {
            return fd.getView();
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
