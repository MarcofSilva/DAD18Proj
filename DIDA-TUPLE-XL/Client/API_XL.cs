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

        private string url;

        public API_XL() {
            _myRemoteObject = new ClientService();
            serverRemoteObjects = prepareForRemoting(ref channel, _myRemoteObject);

            url = "tcp://localhost:8085/ClientService";  //TODO make url dinamic
        }

        public delegate void writeDelegate(ArrayList tuple);
        public delegate void readDelegate(ArrayList tuple);
        public delegate void takeDelegate(ArrayList tuple);

        public override void Write(ArrayList tuple) {
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Write(tuple, url, nonce);
                }
                nonce += 1;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Read(ArrayList tuple) {
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Read(tuple, url, nonce);
                }
                nonce += 1;
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
                    remoteObject.Take(tuple, url, nonce);
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
    }
}