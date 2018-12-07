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
        public List<TupleClass> tupleSpace;

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
        public bool frozen = false;

        public Server(){
            tupleSpaceLock = new ReaderWriterLockSlim();
            prepareRemoting(defaultPort, defaultname, defaultDelay, defaultDelay);
            fd = new FailureDetector(url);
            tupleSpace = fd.updateTS();
        }

        public Server(string URL, string min_delay, string max_delay) {
            url = URL;
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port, imin_delay, imax_delay;
            Int32.TryParse(urlSplit[2], out port);
            Int32.TryParse(min_delay, out imin_delay);
            Int32.TryParse(max_delay, out imax_delay);

            prepareRemoting(port, urlSplit[3], imin_delay, imax_delay);
            fd = new FailureDetector(url);
            Console.WriteLine("Hello! I'm a Server at port " + urlSplit[2]);
            tupleSpace = fd.updateTS();
        }

        private void prepareRemoting(int port, string name, int min_delay, int max_delay) {
            tupleSpace = new List<TupleClass>();
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ServerService(this, min_delay, max_delay);
            RemotingServices.Marshal(myRemoteObject, name, typeof(ServerService)); 
        }

        public void write(TupleClass tuple){
            Console.WriteLine("Operation: Write" + tuple.ToString() + "\n");

            tupleSpaceLock.EnterWriteLock();
            try {
                tupleSpace.Add(tuple);
            }
            finally {
                tupleSpaceLock.ExitWriteLock();
            }
            lock (dummyObjForLock) {
                Monitor.PulseAll(dummyObjForLock);
            }
        }

        public TupleClass read(TupleClass tuple) {
            TupleClass resTuple = null;
            while (resTuple == null) {
                Console.WriteLine("Operation: Read" + tuple.ToString() + "\n");

                tupleSpaceLock.EnterReadLock();
                try {
                    Regex capital = new Regex(@"[A-Z]");
                    foreach (TupleClass t in tupleSpace) {
                        if (t.Matches(tuple)) {
                            resTuple = t;
                            break;
                        }
                    }
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

        public List<TupleClass> takeRead(TupleClass tuple, string clientURL) {
            Console.WriteLine("Operation: Take" + tuple.ToString() + "\n");

            List<TupleClass> res = new List<TupleClass>();
            Regex capital = new Regex(@"[A-Z]");
            List<TupleClass> allTuples = new List<TupleClass>();
            
            lock (toTakeSubset) { //Prevent a take from searching for tuples when another take is already doing it
                if (toTakeSubset.ContainsKey(clientURL))
                {
                    toTakeSubset.Remove(clientURL);
                }
                foreach (List<TupleClass> list in toTakeSubset.Values) {
                    foreach (var y in list) {
                        allTuples.Add(y);
                    }
                }

                foreach (TupleClass el in tupleSpace.ToList()) {
                    if (el.Matches(tuple) && !allTuples.Contains(el)) { //ignores blocked tuples
                        res.Add(el);
                    }
                    else if(el.Matches(tuple) && allTuples.Contains(el)) {
                        return new List<TupleClass>();
                    }
                }
                if (res.Count != 0) {
                    toTakeSubset.Add(clientURL, res);
                }

            }
            if (res.Count == 0) {
                return new List<TupleClass>();
            }
            else {
                return res;
            }
        }

        public void takeRemove(TupleClass tuple, string clientURL) {        
            foreach (TupleClass el in tupleSpace) {
                if(tuple.Equals(el)) {
                    tupleSpaceLock.EnterWriteLock();
                    tupleSpace.Remove(el);
                    tupleSpaceLock.ExitWriteLock();
                    lock (toTakeSubset) {
                        toTakeSubset.Remove(clientURL);
                    }
                    break;
                }
            }
        }

        public void releaseLocks(string clientUrl) {
            lock (toTakeSubset) {
                toTakeSubset.Remove(clientUrl);
            }
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
            tupleSpace = fd.updateTS();
        }

        public int ping() { 
            checkFrozen();
            return 1;
        }

        public List<string> viewRequest() {
            return fd.getView();
        }

        public void status() {
            Console.WriteLine("----------Server status----------");
            Console.WriteLine("--TupleSpace--");
            foreach (TupleClass tuple in tupleSpace) {
                Console.WriteLine(tuple);
            }
            Console.WriteLine("--View--");
            foreach(string s in fd.getView()) {
                Console.WriteLine(s);
            }
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
