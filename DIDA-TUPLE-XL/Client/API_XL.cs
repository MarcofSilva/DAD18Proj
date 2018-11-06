using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace Client {
    public class API_XL : TupleSpaceAPI {

        private TcpChannel channel;
        private ClientService _myRemoteObject;
        private List<IServerService> serverRemoteObjects;

        public API_XL() {
            _myRemoteObject = new ClientService();
            serverRemoteObjects = prepareForRemoting(ref channel, _myRemoteObject);
        }

        public override void Write(ArrayList tuple) {
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Write(tuple, "tcp://localhost:8085/ClientService", nonce); //TODO make url dinamic
                }
                nonce += 1;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Read(ArrayList tuple) {
            //TODO
            //prints para debbug
            //Console.WriteLine("read in API_SMR: ");
            foreach (var item in tuple) {
                if (item != null) {
                    //Console.WriteLine(item.ToString());
                }
                else {
                    Console.WriteLine("null");
                }
            }
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Read(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Take(ArrayList tuple) {
            //TODO
            //prints para debbug
            //Console.Write("take in API_SMR: ");
            foreach (var item in tuple) {
                //Console.WriteLine(item.ToString());
            }
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Take(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
    }
}