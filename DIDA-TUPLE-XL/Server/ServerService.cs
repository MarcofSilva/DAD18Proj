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
        //tem de se fazer lock disto
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        //todo private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();
        private int _min_delay;
        private int _max_delay;
        private Random random = new Random();

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
                //todo _remoteStorage.Add(clientURL, (IClientService)Activator.GetObject(typeof(IClientService), clientURL));
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
            if (validRequest(clientUrl, nonce)) {//success
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("Write Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Write Request");
                _server.write(tuple);
                Console.WriteLine("It's written!");
            }
        }

        public List<TupleClass> Read(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("Read Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Read Request");
                responseTuple = _server.read(tuple);
                if (responseTuple.Count != 0) {
                    foreach (TupleClass t in responseTuple) {
                        Console.WriteLine(t.ToString());
                    }
                }
                return responseTuple;
            }//Update nonce info
            Console.WriteLine("empty read");
            return new List<TupleClass>();
        }

        public List<TupleClass> TakeRead(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("TakeRead Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRead Request");
                responseTuple = _server.takeRead(tuple);
                return responseTuple;
            }//Update nonce info
            return new List<TupleClass>();
        }

        public void TakeRemove(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            if (validRequest(clientUrl, nonce)) {//success
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("TakeRemove Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRemove Request");
                _server.takeRemove(tuple);
            }
        }

        public void Freeze() {
            _server.Freeze();
        }

        public void Unfreeze() {
            _server.Unfreeze();
        }
    }
}
