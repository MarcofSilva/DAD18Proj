using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace Client {
    public abstract class TupleSpaceAPI {
        public abstract void Write(ArrayList tuple);

        public abstract void Read(ArrayList tuple);

        public abstract void Take(ArrayList tuple);

        protected List<IServerService> prepareForRemoting(ref TcpChannel channel, ArrayList serverURLs, int port) {
            System.Collections.IDictionary dict = new System.Collections.Hashtable();
            dict["port"] = port;
            dict["name"] = port.ToString();
            channel = new TcpChannel(dict, null, null); //TODO Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);

            List<IServerService> serverRemoteObjects = new List<IServerService>();
            foreach (string url in serverURLs) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }
            return serverRemoteObjects;
        }
    }
}
