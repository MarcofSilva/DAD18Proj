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

        public ServerService(Server server) {
            _server = server;
        }

        public void Write(ArrayList tuple, string clientURL) {
            _server.write(tuple);
        }

        public void Read(ArrayList tuple, string clientURL) {
            _server.read(tuple);
        }

        public void Take(ArrayList tuple, string clientURL) {
            _server.take(tuple);
        }
    }
}
