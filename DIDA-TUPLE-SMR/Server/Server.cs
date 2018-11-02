using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Collections;
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
        private List<ArrayList> tupleContainer;

        public Server()
        {
            tupleContainer = new List<ArrayList>();
            TcpChannel channel = new TcpChannel(8086); //TODO port
            ChannelServices.RegisterChannel(channel, false);
            ServerService myRemoteObject = new ServerService(this);
            RemotingServices.Marshal(myRemoteObject, "ServService", typeof(ServerService)); //TODO remote object name
            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }

        //void? devolve algo??
        public void write( ArrayList tuple)
        {
            return;
        }

        //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> read(ArrayList tuple)
        {
            return new List<ArrayList>(); 
        }

        //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> take(ArrayList tuple)
        {
            return new List<ArrayList>(); 
        }

        static void Main(string[] args)
        {
            Server server = new Server();
        }
    }
}
