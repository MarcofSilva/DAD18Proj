using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using ClassLibrary;
using System.Threading.Tasks;

namespace Client {
    public abstract class TupleSpaceAPI {
        private ClientService myRemoteObject;

        private long _operationNonce;

        public TupleSpaceAPI() {
            _operationNonce = 0;
        }

        public long nonce {
            get {
                return _operationNonce;
            }

            set {
                _operationNonce = value;
            }
        }

        public abstract void Write(TupleClass tuple);

        public abstract TupleClass Read(TupleClass tuple);

        public abstract TupleClass Take(TupleClass tuple);

        public abstract void freeze();

        public abstract void unfreeze();

        protected List<IServerService> prepareForRemoting(ref TcpChannel channel, string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < urlSplit.Length; i++) {
                Console.WriteLine(urlSplit[i]);
            }
            int port;
            Int32.TryParse(urlSplit[2], out port);

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ClientService(this);
            RemotingServices.Marshal(myRemoteObject, urlSplit[3], typeof(ClientService));

            Console.WriteLine("Hello! I'm a Client at port " + urlSplit[2]);

            List<IServerService> serverRemoteObjects = new List<IServerService>();
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }
            return serverRemoteObjects;
        }
    }
}
