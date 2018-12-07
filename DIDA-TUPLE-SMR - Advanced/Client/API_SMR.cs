using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;
using ExceptionLibrary;
//TODO apagar usings desnecessarios
namespace Client {
    class API_SMR : TupleSpaceAPI {

        private const int defaultPort = 8085;
        private TcpChannel channel;
        private IServerService serverRemoteObject;
        private bool frozen = false;
        private string url;

        public API_SMR(string URL) {
            serverRemoteObject = prepareForRemoting(ref channel, URL);

            url = URL;
        }

        public override void write(TupleClass tuple) {
            checkFrozen();
            try {
                serverRemoteObject.write(tuple,url, nonce);
                nonce++;
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                write(tuple);
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public override TupleClass read(TupleClass tuple) {
            checkFrozen();
            try {
                List<TupleClass> res = serverRemoteObject.read(tuple, url, nonce);
                nonce++;
                return res[0];
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                return read(tuple);
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override TupleClass take(TupleClass tuple) {
            checkFrozen();
            try {
                TupleClass res = serverRemoteObject.take(tuple, url, nonce);
                nonce++;
                return res;
            }
            catch (ElectionException) {
                Thread.Sleep(500);
                return take(tuple);
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void freeze() {
            frozen = true;
        }

        public override void unfreeze() {
            Console.WriteLine("Unfreezing...");
            lock (this) {
                Monitor.PulseAll(this);
            }
            frozen = false;
        }

        public void checkFrozen() {
            if (frozen) {
                Console.WriteLine("Cant do anything, im frozen");
                lock (this) {
                    while (frozen) {
                        Console.WriteLine("Waiting...");
                        Monitor.Wait(this);
                    }
                }
            }
        }
    }
}
