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
using ClassLibrary;

namespace Server {
    public class Server {
        private List<TupleClass> tupleSpace;
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 8086;
        private const string defaultname = "Server";
        private List<IServerService> serverRemoteObjects;

        public Server() {
            prepareRemoting(defaultPort, defaultname);
            Console.WriteLine("Hello! I'm a Server at port " + defaultPort);
        }

        public Server(string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Int32.TryParse(urlSplit[2], out port);

            prepareRemoting(port, urlSplit[3]);
            Console.WriteLine("Hello! I'm a Server at port " + urlSplit[2]);
        }

        private void prepareRemoting(int port, string name) {
            tupleSpace = new List<TupleClass>();
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ServerService(this);
            RemotingServices.Marshal(myRemoteObject, name, typeof(ServerService)); //TODO remote object name

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);

            Console.WriteLine("Hello! I'm a Client at port " + port);

            List<IServerService> serverRemoteObjects = new List<IServerService>();
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }
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
            Regex capital = new Regex(@"[A-Z]");
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
            Regex capital = new Regex(@"[A-Z]");
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
