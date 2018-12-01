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
        private ReaderWriterLockSlim tupleSpaceLock;
        private List<TupleClass> tupleSpace;

        private Object dummyObjForLock; //dummy object for lock and wait and lock and pulse in read and write.
        private Dictionary<string, List<TupleClass>> toTakeSubset = new Dictionary<string, List<TupleClass>>();

        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 8086;
        private const string defaultname = "Server";

        public Server(){
            tupleSpaceLock = new ReaderWriterLockSlim();
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
            Console.WriteLine("Operation: Write" + tuple.ToString() + "\n");

            tupleSpaceLock.EnterWriteLock();
            try {
                //Console.WriteLine("Before write Size: " + tupleSpace.Count + "\n");
                tupleSpace.Add(tuple);
                //Console.WriteLine("Wrote: " + printTuple(tuple) + "\n");
                //Console.WriteLine("After write Size: " + tupleSpace.Count + "\n");
            }
            finally {
                tupleSpaceLock.ExitWriteLock();
            }
            lock (dummyObjForLock) {//TODO sera necessario contador com numero de readers em wait para ir decrementando quando se faz pulse isto se se fizer apenas pulse no write e no read e take read se fizer tbm pulse até que se acordem todos os readers em wait de forma ordenada
                Monitor.PulseAll(dummyObjForLock);//TODO secalhar e melhor ir fazendo pulse um a um, para manter a ordem?
            }
        }

        public TupleClass read(TupleClass tuple) {
            TupleClass resTuple = null;
            while (resTuple == null) {
                Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");

                tupleSpaceLock.EnterReadLock(); //TODO NullReferenceException xDDD
                try {
                    //Console.WriteLine("initial read " + tupleContainer.Count + " container");
                    Regex capital = new Regex(@"[A-Z]");
                    foreach (TupleClass t in tupleSpace) {
                        if (t.Matches(tuple)) {
                            resTuple = t;
                            break;
                        }
                    }
                    //Console.WriteLine("Server : Read TupleSpace Size: " + tupleSpace.Count + "\n");
                }
                finally {
                    tupleSpaceLock.ExitReadLock();
                }
                if (resTuple == null) {
                    lock (dummyObjForLock) {
                        Monitor.Wait(dummyObjForLock);
                    }
                }
            }
            return resTuple;
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
