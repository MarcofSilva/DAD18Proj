using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using System.Threading;

namespace Server{
    public class Server{
        private List<TupleClass> tupleSpace;
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 8086;
        private const string defaultname = "Server";
        private const int defaultDelay = 0;
        //readonly object _Key = new object();
        public bool frozen = false;

        public Server(){
            prepareRemoting(defaultPort, defaultname, defaultDelay, defaultDelay);
        }

        public Server(string URL, string min_delay, string max_delay) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port, imin_delay, imax_delay;
            Int32.TryParse(urlSplit[2], out port);
            Int32.TryParse(min_delay, out imin_delay);
            Int32.TryParse(max_delay, out imax_delay);

            prepareRemoting(port, urlSplit[3], imin_delay, imax_delay);
            Console.WriteLine("Hello! I'm a Server at port " + urlSplit[2]);
        }

        private void prepareRemoting(int port, string name, int min_delay, int max_delay) {
            tupleSpace = new List<TupleClass>();
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ServerService(this, min_delay, max_delay);
            RemotingServices.Marshal(myRemoteObject, name, typeof(ServerService)); //TODO remote object name
        }

        public void write(TupleClass tuple){
            Console.WriteLine("Operation: Write" + tuple.ToString() + "\n");
            //Console.WriteLine("Before write Size: " + tupleSpace.Count + "\n");
            tupleSpace.Add(tuple);
            Console.WriteLine("Wrote: " + tuple.ToString() + "\n");
            //Console.WriteLine("After write Size: " + tupleSpace.Count + "\n");
        }

        public void takeRemove(TupleClass tuple) {
            //Console.WriteLine("----->DEBUG_Server: tuple to delete " + printTuple(tuple));
            //Console.WriteLine("Trying to delete Size: " + tupleSpace.Count + "\n");
            foreach (TupleClass el in tupleSpace) {
                if(tuple.Equals(el)) {
                    Console.WriteLine("----->DEBUG_Server: deleted " + el.ToString());
                    tupleSpace.Remove(el);
                    //Console.WriteLine("Deleted Size: " + tupleSpace.Count + "\n");
                    return;
                }
            }
        }

        //e basicamente igual ao read mas com locks nas estruturas
        public List<TupleClass> takeRead(TupleClass tuple) {
            Console.WriteLine("Operation: Take" + tuple.ToString() + "\n");
            List<TupleClass> res = new List<TupleClass>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (TupleClass el in tupleSpace) {
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            return res; //no match
        }

        public List<TupleClass> read(TupleClass tuple){
            Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");
            List<TupleClass> res = new List<TupleClass>();
            //Console.WriteLine("initial read " + tuple.Size + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (TupleClass el in tupleSpace){
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            //Console.WriteLine("Server : Read TupleSpace Size: " + tupleSpace.Count + "\n");
            return res; //no match
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

        static void Main(string[] args){
            Server server;
            if(args.Length == 0) {
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
