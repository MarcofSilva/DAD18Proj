using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService {
    class PCS {
        private const int PCS_DEFAULT_PORT = 10000;

        private TcpChannel channel;
        private PCS_Service myRemoteObject;

        public PCS() {
            channel = new TcpChannel(PCS_DEFAULT_PORT);
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new PCS_Service(this);
            RemotingServices.Marshal(myRemoteObject, "PCS_Service", typeof(PCS_Service));
        }

        static void Main(string[] args) {
            PCS _pcs = new PCS();
            while (true) {
                Console.WriteLine("Quit to stop...");
                string line = Console.ReadLine();
                if (line.Equals("quit")) {
                    break;
                }
            }
        }
    }
}
