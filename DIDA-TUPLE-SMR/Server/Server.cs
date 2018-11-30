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
        public string _url = "tcp://localhost:8086/Server";

        //public so server services can acess it
        //alternativa 
        public RaftState _state;

        private List<TupleClass> tupleSpace = new List<TupleClass>();
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private int _port = 8086;
        private string _name = "Server";
        //private List<IServerService> serverRemoteObjects;
        private int _numServers = 0;


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

        private void selfPrepare() {
            serverRemoteObjects = new Dictionary<string, IServerService>();

            channel = new TcpChannel(_port);
            ChannelServices.RegisterChannel(channel, false);

            myRemoteObject = new ServerService(this);
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

            //martelo para teste
            if(_port == 8086) {
                Console.WriteLine("I am server with port:" +_port +" i am have a leader");
                leader = new LeaderState(this, _numServers);
                _state = leader;
            }
            else {
                Console.WriteLine("I am server with port:" + _port + " i am have a follower");
                follower = new FollowerState(this, _numServers);
                _state = candidate;
            }
            candidate = new CandidateState(this, _numServers);
        }

        public Server() {
            selfPrepare();
        }

        public Server(string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Int32.TryParse(urlSplit[2], out port);
            _port = port;
            _name = urlSplit[3];
            _url = URL;
            selfPrepare();
        }

        public string heartBeat() {
            string res = "Hello from server: " + _name + " at port: " + _port.ToString();
            return res;
        }

        public void write(TupleClass tuple) {
            Console.WriteLine("Operation: Write" + tuple.ToString() + "\n");
            //Console.WriteLine("Before write Size: " + tupleSpace.Count + "\n");
            tupleSpace.Add(tuple);
            //Console.WriteLine("Wrote: " + printTuple(tuple) + "\n");
            //Console.WriteLine("After write Size: " + tupleSpace.Count + "\n");
        }

        public List<TupleClass> take(TupleClass tuple) {
            Console.WriteLine("Operation: Take" + tuple.ToString() + "\n");
            List<TupleClass> res = new List<TupleClass>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res.Add(el);
                    res.Remove(el);
                    return res;
                }
            }
            return res; //no match
        }

        public List<TupleClass> read(TupleClass tuple) {
            Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");
            List<TupleClass> res = new List<TupleClass>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            //Console.WriteLine("Server : Read TupleSpace Size: " + tupleSpace.Count + "\n");
            return res; //no match
        }

        static void Main(string[] args) {
            Server server;
            if (args.Length == 0) {
                server = new Server();
            }
            else {
                server = new Server(args[0]);
            }
            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }
    }
}
