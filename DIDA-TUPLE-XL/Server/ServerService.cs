using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class ServerService : MarshalByRefObject, IServerService
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

        public void Write(ArrayList tuple, string clientURL, long nonce) {
            Console.WriteLine("----->DEBUG_ServerSerice: Received Write Request CARALHO");
            if (validRequest(clientURL, nonce)) {//success
                _server.write(tuple);
            }
        }

        public List<ArrayList> Read(ArrayList tuple, string clientURL, long nonce) {
            Console.WriteLine("----->DEBUG_ServerSerice: Received Read Request");
            List<ArrayList> responseTuple = new List<ArrayList>();
            if (validRequest(clientURL, nonce)) {
                responseTuple = _server.read(tuple);
                return responseTuple;
            }//Update nonce info
            return new List<ArrayList>();
        }

        public List<ArrayList> TakeRead(ArrayList tuple, string clientURL, long nonce) {
            Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRead Request");
            List<ArrayList> responseTuple = new List<ArrayList>();
            if (validRequest(clientURL, nonce)) {
                responseTuple = _server.takeRead(tuple);
                return responseTuple;
            }//Update nonce info
            return new List<ArrayList>();
        }

        public void TakeRemove(ArrayList tuple, string clientURL, long nonce) {
            Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRemove Request");
            if (validRequest(clientURL, nonce)) {//success
                _server.takeRemove(tuple);
            }
        }
    }
}
