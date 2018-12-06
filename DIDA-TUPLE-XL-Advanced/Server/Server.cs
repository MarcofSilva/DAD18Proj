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
using System.Collections.Concurrent;

namespace Server{
    public class Server{
        private ReaderWriterLockSlim tupleSpaceLock = new ReaderWriterLockSlim();
        private List<TupleClass> tupleSpace;

        private Object dummyObjForLock = new Object(); //dummy object for lock and wait and lock and pulse in read and write.
        private Object dummyObjForTakeRead = new object();
        private Dictionary<string, List<TupleClass>> toTakeSubset = new Dictionary<string, List<TupleClass>>();
        private FailureDetector fd;

        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 50000;
        private const string defaultname = "S";
        private const int defaultDelay = 0;
        public string url;
        //readonly object _Key = new object();
        public bool frozen = false;

        public Server(){
            tupleSpaceLock = new ReaderWriterLockSlim();
            prepareRemoting(defaultPort, defaultname, defaultDelay, defaultDelay);
            fd = new FailureDetector();
        }

        public Server(string URL, string min_delay, string max_delay) {
            url = URL;
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port, imin_delay, imax_delay;
            Int32.TryParse(urlSplit[2], out port);
            Int32.TryParse(min_delay, out imin_delay);
            Int32.TryParse(max_delay, out imax_delay);

            prepareRemoting(port, urlSplit[3], imin_delay, imax_delay);
            fd = new FailureDetector();
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

            tupleSpaceLock.EnterWriteLock();
            try {
                tupleSpace.Add(tuple);
                //Console.WriteLine("Wrote: " + printTuple(tuple) + "\n");
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

                tupleSpaceLock.EnterReadLock(); // TODO can you read blocked tuples (by take)?
                try {
                    //Console.WriteLine("initial read " + tupleContainer.Count + " container");
                    Regex capital = new Regex(@"[A-Z]");
                    lock (tupleSpace) {
                        foreach (TupleClass t in tupleSpace) {
                            if (t.Matches(tuple)) {
                                resTuple = t;
                                break;
                            }
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
        public List<TupleClass> takeRead(TupleClass tuple, string clientURL) {
            Console.WriteLine("Operation: Take" + tuple.ToString() + " from " + clientURL + "\n");
            Console.WriteLine("Antes -> ");
            foreach (var x in tupleSpace) {
                Console.WriteLine("-> " + x.ToString());
            }
            if (toTakeSubset.ContainsKey(clientURL)) {
                toTakeSubset.Remove(clientURL);
            }

            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            List<TupleClass> res = new List<TupleClass>();
            lock (toTakeSubset) { //Prevent a take to search for tuples when another take is already doing it
                List<TupleClass> allTuples = new List<TupleClass>();
                
                Console.WriteLine("totakesubset1 -> ");
                foreach (var x in toTakeSubset) {
                    Console.WriteLine(x.Key + "->" + x.Value.ToString());
                }
                foreach (List<TupleClass> list in toTakeSubset.Values) {
                    foreach (var y in list) {
                        allTuples.Add(y);
                    }
                }
                Console.WriteLine("alltuples -> ");
                foreach (var x in allTuples) {
                    Console.WriteLine("-> " + x.ToString());
                }
                lock (tupleSpace) {
                    foreach (TupleClass el in tupleSpace.ToList()) {
                        //Console.WriteLine(el.ToString() + " ----- " + tuple.ToString());
                        if (el.Matches(tuple) && !allTuples.Contains(el)) { //ignora os bloqueados
                            res.Add(el);
                        }
                    }
                    if (res.Count != 0) {
                        toTakeSubset.Add(clientURL, res);
                    }
                }
                
                Console.WriteLine("totakesubset2 -> ");
                foreach (var x in toTakeSubset) {
                    Console.WriteLine(x.Key + "->" + x.Value.ToString());
                }
            }
            if (res.Count == 0) {
                Console.WriteLine("empty res");
                lock (dummyObjForLock) {
                    Monitor.Wait(dummyObjForLock);
                } //no match
                return takeRead(tuple, clientURL);
            }
            else {
                foreach (TupleClass t in res) {
                    Console.WriteLine("res -> " + t.ToString());
                }
                return res;
            }
        }

        public void takeRemove(TupleClass tuple, string clientURL) {        
            //Console.WriteLine("----->DEBUG_Server: tuple to delete " + tuple.ToString());
            foreach (TupleClass el in tupleSpace) {
                if(tuple.Equals(el)) {
                    Console.WriteLine(tuple.ToString() + " -- " + el.ToString());
                    //Console.WriteLine("----->DEBUG_Server: deleted " + printTuple(el));
                    lock (tupleSpace) {
                        tupleSpace.Remove(el);
                    }
                    //Console.WriteLine(Thread.CurrentThread.ManagedThreadId + " " + success); TODO
                    //Console.WriteLine("Deleted Size: " + tupleSpace.Count + "\n");
                    lock (toTakeSubset) {
                        toTakeSubset.Remove(clientURL);
                    }
                    lock (dummyObjForLock) {
                        Monitor.PulseAll(dummyObjForLock);
                    }
                    break;
                }
            }
            /*foreach(var x in tupleSpace) {
                Console.WriteLine("Depois -> " + x.ToString());
            }*/
        }

        public void Freeze() {
            Console.WriteLine("I'm freezing");
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

        public int ping() { //TODO put this only on serverservice?
            checkFrozen();
            return 1;
        }

        public List<string> viewRequest() {
            return fd.getView();
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
