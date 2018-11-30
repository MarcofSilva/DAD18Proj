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
        private RaftState _state = new CandidateState();
        private List<TupleClass> tupleSpace = new List<TupleClass>();
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private int _port = 8086;
        private string _name = "Server";
        private List<IServerService> serverRemoteObjects;
        private int _numServers = 0;
        private string _url = "tcp://localhost:8086/Server";
        private Random rnd = new Random();
        private System.Timers.Timer timer;


        private void selfPrepare() {
            SetTimer();
            Console.WriteLine(_port);
            Console.WriteLine(_name);

            serverRemoteObjects = new List<IServerService>();

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
                    serverRemoteObjects.Add((ServerService)Activator.GetObject(typeof(ServerService), url));
                }
            }
            _numServers = serverRemoteObjects.Count;
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

        public delegate string heartBeatDelegate();

        private void pulseHeartbeat() {
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                for (int i = 0; i < _numServers; i++) {
                    ServerService remoteObject = (ServerService)serverRemoteObjects[i];
                    heartBeatDelegate heartBeatDel = new heartBeatDelegate(remoteObject.heartBeat);
                    IAsyncResult ar = heartBeatDel.BeginInvoke(null, null) ;
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {
                   pulseHeartbeat();
                }
                else {
                    for (int i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        string response = heartBeatDel.EndInvoke(asyncResult);
                        Console.WriteLine(response);
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
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

        public void terminateClock() {
            timer.Stop();
            timer.Dispose();
        }
        private void SetTimer() {
            int wait = rnd.Next(250, 350);
            Console.WriteLine("----------->WAITING :" + wait);
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
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
            server.terminateClock();
        }
    }
}
