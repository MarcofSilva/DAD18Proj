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

namespace Server{
    public class Server{
        private List<TupleClass> tupleSpace;
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 8086;
        private const string defaultname = "Server";

        public Server(){
            prepareRemoting(defaultPort, defaultname);
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
        }

        public void write(TupleClass tuple){
            //Console.WriteLine("Before write Size: " + tupleSpace.Count + "\n");
            tupleSpace.Add(tuple);
            //Console.WriteLine("Wrote: " + printTuple(tuple) + "\n");
            //Console.WriteLine("After write Size: " + tupleSpace.Count + "\n");
        }

        public void takeRemove(TupleClass tuple) {        
            //Console.WriteLine("----->DEBUG_Server: tuple to delete " + printTuple(tuple));
            //Console.WriteLine("Trying to delete Size: " + tupleSpace.Count + "\n");
            foreach (TupleClass el in tupleSpace) {
                if(tuple.Equals(el)) {
                    //Console.WriteLine("----->DEBUG_Server: deleted " + printTuple(el));
                    tupleSpace.Remove(el);
                    //Console.WriteLine("Deleted Size: " + tupleSpace.Count + "\n");
                    return;
                }
            }
        }

        //e basicamente igual ao read mas com locks nas estruturas
        public List<TupleClass> takeRead(TupleClass tuple) {
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
            
            List<TupleClass> res = new List<TupleClass>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (TupleClass el in tupleSpace){
                if (el.Matches(tuple)) {
                    res.Add(el);
                }
            }
            Console.WriteLine("Server : Read TupleSpace Size: " + tupleSpace.Count + "\n");
            return res; //no match
        }

        static void Main(string[] args){
            Server server;
            if(args.Length == 0) {
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
