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

        protected List<IServerService> prepareForRemoting(ref TcpChannel channel, ArrayList serverURLs, string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Int32.TryParse(urlSplit[2], out port);

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);

            Console.WriteLine("Hello! I'm a Client at port " + urlSplit[2]);

            List<IServerService> serverRemoteObjects = new List<IServerService>();
            foreach (string url in serverURLs) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }
            return serverRemoteObjects;
        }
    }
}
