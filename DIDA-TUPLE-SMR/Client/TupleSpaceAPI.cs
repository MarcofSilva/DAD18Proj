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
using System.Threading.Tasks;
using ClassLibrary;

namespace Client {
    public abstract class TupleSpaceAPI {

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

        public abstract void write(TupleClass tuple);

        public abstract TupleClass read(TupleClass tuple);

        public abstract TupleClass take(TupleClass tuple);

        public abstract void freeze();

        public abstract void unfreeze();


        protected IServerService prepareForRemoting(ref TcpChannel channel, string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Console.WriteLine(URL);

            Int32.TryParse(urlSplit[2], out port);

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);

            Console.WriteLine("Hello! I'm a Client at port " + urlSplit[2]);

            //TODO nao usar allkeys
            string url = ConfigurationManager.AppSettings.AllKeys[0];

            IServerService serverRemoteObject = ((IServerService)Activator.GetObject(typeof(IServerService), url));
            
            return serverRemoteObject;
        }
    }
}
