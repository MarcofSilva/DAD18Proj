using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Configuration;
using System.Threading;

namespace Server
{
    class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;
        private int _min_delay;
        private int _max_delay;
        private Random random = new Random();

        //tem de se fazer lock disto
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        //todo private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();

        public ServerService(Server server, int min_delay, int max_delay) {
            _server = server;
            _min_delay = min_delay;
            _max_delay = max_delay;
        }

        //TODOOOOOOOO
        //necessario nounces no smr?
        private bool validRequest(string clientURL, long nonce) {
            //se nunca apareceu vai ser adicionado
            return true;
            /*
            if (!_nonceStorage.ContainsKey(clientURL)) {
                _nonceStorage.Add(clientURL, nonce);
                //todo _remoteStorage.Add(clientURL, (IClientService)Activator.GetObject(typeof(IClientService), clientURL));
                return true;
            }
            else {//ja apareceu
                long o = _nonceStorage[clientURL];
                if (nonce > o) {
                    _nonceStorage[clientURL] = nonce;
                    return true;
                }
                return false;
            }*/
        }

        public List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("Read Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Read Request");
                responseTuple = _server.read(tuple, clientUrl, nonce);
                //Console.WriteLine("----->DEBUG_ServerSerice: " + responseTuple[0].ToString());
                return responseTuple;
            }//Update nonce info
            return new List<TupleClass>();
        }

        public List<TupleClass> take(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("TakeRead Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRead Request");
                responseTuple = _server.take(tuple, clientUrl, nonce);
                return responseTuple;
            }//Update nonce info
            return new List<TupleClass>();
        }

        public void write(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            if (validRequest(clientUrl, nonce)) {//success
                int r = random.Next(_min_delay, _max_delay);
                Console.WriteLine("Write Network Delay: " + r.ToString());
                Thread.Sleep(r);
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Write Request");
                _server.write(tuple, clientUrl, nonce);
                Console.WriteLine("It's written!");
            }
        }
        public string heartBeat(int term, string candidateID) {
            return _server.heartBeat(term, candidateID);
        }

        public bool vote(int term, string candidateID) {
            return _server.vote(term, candidateID);
        }
        public void Freeze() {
            _server.Freeze();
        }

        public void Unfreeze() {
            _server.Unfreeze();
        }
    }
}
