using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary {
    public class API_XL : TupleSpaceAPI {
        //TODO estupidamente mal aqui, só para testar o resto...nao consegui usar o configuration file
        private ArrayList serverURLs;

        private TcpChannel channel;
        List<IServerService> serverRemoteObjects;

        public API_XL() {
            //TODO estupidamente mal aqui, só para testar o resto...nao consegui usar o configuration file
            serverURLs = new ArrayList();
            serverURLs.Add("tcp://localhost:8086/ServService");

            serverRemoteObjects = prepareForRemoting(ref channel, serverURLs);
        }

        public override void Write(ArrayList tuple) {
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Write(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Read(ArrayList tuple) {
            //TODO
            //prints para debbug
            Console.WriteLine("read: ");
            foreach (var item in tuple) {
                if (item != null) {
                    Console.WriteLine(item.ToString());
                }
                else {
                    Console.WriteLine("null");
                }
            }
        }

        public override void Take(ArrayList tuple) {
            //TODO
            //prints para debbug
            Console.Write("take: ");
            foreach (var item in tuple) {
                Console.WriteLine(item.ToString());
            }
        }
    }
}