using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;

namespace Client {
    class API_SMR : TupleSpaceAPI {

        private const int defaultPort = 8085;
        private TcpChannel channel;
        private List<IServerService> serverRemoteObjects;
        private int numServers;

        private string url;

        public API_SMR(string URL) {
            serverRemoteObjects = prepareForRemoting(ref channel, URL);
            numServers = serverRemoteObjects.Count;

            url = URL;
        }

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeDelegate(TupleClass tuple, string url, long nonce);
        //public delegate void takeRemoveDelegate(ArrayList tuple, string url, long nonce);

        public override void Write(TupleClass tuple) {
            throw new NotImplementedException();
        }
        public override TupleClass Read(TupleClass tuple) {
            throw new NotImplementedException();
        }

        public override TupleClass Take(TupleClass tuple) {
            throw new NotImplementedException();
        }
    }
}
