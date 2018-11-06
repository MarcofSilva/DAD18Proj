using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;
        //tem de se fazer lock disto
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();

        public ServerService(Server server) {
            _server = server;
        }

        private bool validRequest(string clientURL, long nonce) {
            //se nunca apareceu vai ser adicionado
            if (!_nonceStorage.ContainsKey(clientURL)) {
                _nonceStorage.Add(clientURL, nonce);
                _remoteStorage.Add(clientURL, (IClientService)Activator.GetObject(typeof(IClientService), clientURL));
                return true;
            }
            else {//ja apareceu
                long o;
                if (_nonceStorage.TryGetValue(clientURL, out o)) {
                    if (nonce > o) {
                        _nonceStorage.Remove(clientURL);
                        _nonceStorage.Add(clientURL, nonce);
                        return true;
                    }
                    return false;
                }
            }
            //never reaches here
            return false;
        }

        public void Write(ArrayList tuple, string clientURL, long nonce) {
            ArrayList ack = new ArrayList();
            if (validRequest(clientURL,nonce)) {//sucess
                _server.write(tuple);
                ack.Add(true);
                ack.Add(nonce);
            }
            else {//request duplicated
                ack.Add(false);
            }
            _remoteStorage[clientURL].WriteResponse(ack);
        }

        public void Read(ArrayList tuple, string clientURL, long nonce) {
            ArrayList ack = new ArrayList();
            List<ArrayList> res = new List<ArrayList>();
            if (validRequest(clientURL, nonce)) {//sucess
                ack.Add(true);
                ack.Add(nonce);
                res = _server.read(tuple);
            }
            else {//request duplicated
                ack.Add(false);
            }
            _remoteStorage[clientURL].ReadResponse(ack, res);
        }

        public void Take(ArrayList tuple, string clientURL, long nonce) {
            ArrayList ack = new ArrayList();
            List<ArrayList> res = new List<ArrayList>();
            if (validRequest(clientURL, nonce)) {//sucess
                ack.Add(true);
                ack.Add(nonce);
                res = _server.take(tuple);
            }
            else {//request duplicated
                ack.Add(false);
            }
            _remoteStorage[clientURL].TakeResponse(ack, res);
        }
    }
}
