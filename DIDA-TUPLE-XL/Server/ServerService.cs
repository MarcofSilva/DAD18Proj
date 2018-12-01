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
    public class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;
        //TODO tem de se fazer lock disto
        // O mesmo cliente nunca vai fazer dois pedidos concorrentes pois executa os seus pedidos de forma sincrona
        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        //TODO   private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();

        public ServerService(Server server) {
            _server = server;
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
            if (validRequest(clientUrl, nonce)) {//success
                //Console.WriteLine("----->DEBUG_ServerSerice: Received Write Request");
                _server.write(tuple);
            }
        }

        public TupleClass Read(TupleClass tuple, string clientUrl, long nonce) {
            if (validRequest(clientUrl, nonce)) {
                //Console.WriteLine("----->DEBUG_ServerService: Received Read Request");
                return _server.read(tuple);
            }
            return null;
        }

        public List<TupleClass> TakeRead(TupleClass tuple, string clientUrl) {
            List<TupleClass> responseTuple = new List<TupleClass>();
            //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRead Request");
            responseTuple = _server.takeRead(tuple, clientUrl);
            return responseTuple;
        }

        public void TakeRemove(TupleClass tuple, string clientUrl, long nonce) {
            if (validRequest(clientUrl, nonce)) {//success
                //Console.WriteLine("----->DEBUG_ServerSerice: Received TakeRemove Request");
                _server.takeRemove(tuple, clientUrl);
            }
        }
    }
}
