using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using System.Threading;

namespace Server
{
    public class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        private int _min_delay;
        private int _max_delay;
        private Random random = new Random();
        bool askingView = false;
        bool ready = false;
        object dummy = new Object();
        int numRequests = 0;

        public ServerService(Server server, int min_delay, int max_delay) {
            _server = server;
            _min_delay = min_delay;
            _max_delay = max_delay;
            Console.WriteLine("min : " + min_delay.ToString() + " max: " + max_delay.ToString());
        }

        private bool validRequest(string clientUrl, long nonce) {
            //se nunca apareceu vai ser adicionado
            if (!_nonceStorage.ContainsKey(clientUrl)) {
                _nonceStorage.Add(clientUrl, nonce);
                return true;
            }
            else {//ja apareceu
                long o = _nonceStorage[clientUrl];
                if (nonce > o) {
                    _nonceStorage[clientUrl] = nonce;
                    return true;
                }
                return false;
            }
        }

        public void Write(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            Interlocked.Increment(ref numRequests);
            if (validRequest(clientUrl, nonce)) {//success
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("Write Network Delay: " + r.ToString());
                Thread.Sleep(r);
                _server.write(tuple);
                Console.WriteLine("It's written!");
            }
            Interlocked.Decrement(ref numRequests);
        }

        public TupleClass Read(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            Interlocked.Increment(ref numRequests);
            int r = random.Next(_min_delay, _max_delay);
            Console.WriteLine("Read Network Delay: " + r.ToString());
            Thread.Sleep(r);
            Interlocked.Decrement(ref numRequests);
            return _server.read(tuple);
        }

        public List<TupleClass> TakeRead(TupleClass tuple, string clientUrl) {
            _server.checkFrozen();
            Interlocked.Increment(ref numRequests);
            List<TupleClass> responseTuple = new List<TupleClass>();
            int r = random.Next(_min_delay, _max_delay);
            Console.WriteLine("TakeRead Network Delay: " + r.ToString());
            Thread.Sleep(r);
            responseTuple = _server.takeRead(tuple, clientUrl);
            Interlocked.Decrement(ref numRequests);
            return responseTuple;
       }

        public void TakeRemove(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            Interlocked.Increment(ref numRequests);
            if (validRequest(clientUrl, nonce)) {//success
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("TakeRemove Network Delay: " + r.ToString());
                Thread.Sleep(r);
                _server.takeRemove(tuple, clientUrl);
            }
            Interlocked.Decrement(ref numRequests);
        }
        public void Status() {
            _server.status();
        }
        public void Freeze() {
            _server.Freeze();
        }

        public void Unfreeze() {
            _server.Unfreeze();
        }

        public int Ping() {
            _server.checkFrozen();
            return _server.ping();
        }

        public List<TupleClass> askUpdate() {
            if (numRequests > 0) {
                askingView = true;
                lock (dummy) {
                    while (askingView) {
                        Console.WriteLine("Waiting...");
                        Monitor.Wait(dummy);
                    }
                }
            }
            return _server.getTupleSpace();
        }

        public void checkAskUpdate() {
            while (true) {
                if (numRequests <= 0) {
                    lock (dummy) {
                        Monitor.PulseAll(dummy);
                    }
                    askingView = false;
                }
            }
        }

        public List<string> ViewRequest() {
            _server.checkFrozen(); 
            return _server.viewRequest();
        }

        public void releaseLocks(string clientUrl) {
            _server.releaseLocks(clientUrl);
        }
    }
}
