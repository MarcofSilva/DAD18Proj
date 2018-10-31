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

namespace ClientLibrary
{
    public class TupleSpaceAPI
    {
        public TupleSpaceAPI()
        {   //TODO connect to all available servers that it should connect
            TcpChannel _channel = new TcpChannel(); //TODO Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(_channel, false);
            IServerService _servRemoteObject = (IServerService)Activator.GetObject(typeof(IServerService), "tcp://localhost:8086/ServService"); //TODO IP Address and port of servers
        }
            
        public void Write(ArrayList tuple)
        {
            //TODO
            //prints para debbug
            Console.Write("write: ");
            foreach (var item in tuple)
            {
                Console.WriteLine(item.ToString());
            }
        }

        public void Read(ArrayList tuple)
        {
            //TODO
            //prints para debbug
            Console.Write("read: ");
            foreach (var item in tuple)
            {
                Console.WriteLine(item.ToString());
            }
        }

        public void Take(ArrayList tuple)
        {
            //TODO
            //prints para debbug
            Console.Write("take: ");
            foreach (var item in tuple)
            {
                Console.WriteLine(item.ToString());
            }
        }
    }
}
