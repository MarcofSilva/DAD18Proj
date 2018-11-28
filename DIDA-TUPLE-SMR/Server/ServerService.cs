using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server
{
    class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;

        //tem de se fazer lock disto
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        //todo private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();

        public ServerService(Server server) {
            _server = server;
        }

        private bool validRequest(string clientURL, long nonce) {
            //se nunca apareceu vai ser adicionado
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
            }
        }

        public List<TupleClass> Read(TupleClass tuple, string clientUrl, long nonce) {
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Read Request");
                responseTuple = _server.read(tuple);
                return responseTuple;
            }//Update nonce info
            return new List<TupleClass>();
        }

        public List<TupleClass> Take(TupleClass tuple, string clientUrl, long nonce) {
            List<TupleClass> responseTuple = new List<TupleClass>();
            if (validRequest(clientUrl, nonce)) {
                //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRead Request");
                responseTuple = _server.take(tuple);
                return responseTuple;
            }//Update nonce info
            return new List<TupleClass>();
        }

        public void Write(TupleClass tuple, string clientUrl, long nonce) {
            if (validRequest(clientUrl, nonce)) {//success
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Write Request");
                _server.write(tuple);
            }
        }
    }
}
