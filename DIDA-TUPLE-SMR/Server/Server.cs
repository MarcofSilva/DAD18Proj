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

        //not sure about this
        private CandidateState candidate;
        private FollowerState follower;
        private LeaderState leader;

        // public so states can acess it, 
        // alternative: send the map to states or even the map being created in the states
        public Dictionary<string, IServerService> serverRemoteObjects;
        //public so state leader can acess it,
        //alternative: send it in constructor of leader
        public string _url = "tcp://localhost:8086/S";

        //public so server services can acess it
        //alternativa 
        public RaftState _state;
        public string _name = "Server";
        private int _port = 8086;

        private List<TupleClass> tupleSpace = new List<TupleClass>();
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private int _numServers = 0;
        public bool frozen = false;
        private const int defaultDelay = 0;


        /*. The leader handles all client requests (if a client contacts a follower, the follower redirects it to the leader).
            Current terms are exchanged whenever servers communicate; if one server’s current term is smaller than the other’s, 
                then it updates its current term to the larger value.
            If a candidate or leader discovers that its term is out of date, it immediately reverts to follower state. 
            If a server receives a request with a stale term number, it rejects the request
            To prevent split votes in the first place, election timeouts arechosen randomly from a fixed interval (e.g., 150–300ms).
            Each candidate restarts its randomized election timeout at the start of an election, and it waits for 
                that timeout to elapse before starting the next election;

            The leader appends the command to its log as a new entry, then issues AppendEntries RPCs in parallel to each of the other
            servers to replicate the entry. When the entry has been safely replicated (as described below), the leader applies
            the entry to its state machine and returns the result of that execution to the client. If followers crash or run slowly,
            or if network packets are lost, the leader retries AppendEntries RPCs indefinitely (even after it has responded to
            the client) until all followers eventually store all log entries.

        */

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

        public void updateState(string state, int term, string url) {
            if (state == "follower") {
                _state.stopClock();
                Console.WriteLine("I am now a Follower");
                _state = follower;
            }
            else if(state == "candidate") {
                _state.stopClock();
                Console.WriteLine("I am now a Candidate");
                _state = candidate;
            }
            else {
                _state.stopClock();
                Console.WriteLine("I am now a Leader");
                _state = leader;
            }
            _state.startClock(term, url);
        }

        public bool vote(int term, string candidateID) {
            return _state.vote(term, candidateID);
        }

        public void writeLeader(TupleClass tuple) {
            Console.WriteLine("Operation: Add" + tuple.ToString() + "\n");
            tupleSpace.Add(tuple);
        }
        public TupleClass takeLeader(TupleClass tuple) {
            Console.WriteLine("Operation: Take" + tuple.ToString() + "\n");
            TupleClass res = new TupleClass();
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res = el;
                    tupleSpace.Remove(el);
                    return res;
                }
            }
            return res; //no match
        }
        public List<TupleClass> readLeader(TupleClass tuple) {
            Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");
            List<TupleClass> res = new List<TupleClass>();
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            return res;
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
        public string heartBeat(int term, string candidateID) {
            _state.heartBeat(term, candidateID);
            string res = "Hello from server: " + _name + " at port: " + _port.ToString();
            return res;
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
