using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Server
    {
        static void Main(string[] args)
        {
            TcpChannel channel = new TcpChannel(8086); //TODO port
            ChannelServices.RegisterChannel(channel, false);
            ServerService myRemoteObject = new ServerService();
            RemotingServices.Marshal(myRemoteObject, "ServService", typeof(ServerService)); //TODO remote object name
            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }
    }
}
