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
        private Dictionary<string, long> _nocestorage = new Dictionary<string, long>();

        public ServerService(Server server) {
            _server = server;
        }

        private bool validRequest(string clientURL, long nonce) {
            //se nunca apareceu vai ser adicionado
            if (!_nocestorage.ContainsKey(clientURL)) {
                _nocestorage.Add(clientURL, nonce);
                return true;
            }
            else {//ja apareceu
                long a;
                if (_nocestorage.TryGetValue(clientURL, out a)) {
                    if (nonce > a) {
                        _nocestorage.Remove(clientURL);
                        _nocestorage.Add(clientURL, nonce);
                        return true;
                    }
                    return false;
                }
            }
            //never reaches here
            return false;
        }

        public void Write(ArrayList tuple, string clientURL, long nonce) {
            if (validRequest(clientURL,nonce)) {//sucess
                _server.write(tuple);
            }
            else {//request duplicated

            }
        }

        public void Read(ArrayList tuple, string clientURL, long nonce) {
            if (validRequest(clientURL, nonce)) {//sucess
                _server.take(tuple);
            }
            else {//request duplicated

            }
        }

        public void Take(ArrayList tuple, string clientURL, long nonce) {
            if (validRequest(clientURL, nonce)) {//sucess
                _server.take(tuple);
            }
            else {//request duplicated

            }
        }
    }
}
