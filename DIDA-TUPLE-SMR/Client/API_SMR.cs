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

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeDelegate(TupleClass tuple, string url, long nonce);
        //public delegate void takeRemoveDelegate(ArrayList tuple, string url, long nonce);

        public override void write(TupleClass tuple) {
            checkFrozen();
            //Console.WriteLine("-->DEBUG:  API_SMR write");
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                writeDelegate takeDel = new writeDelegate(serverRemoteObject.write);
                asyncResults[0] = takeDel.BeginInvoke(tuple, url, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    write(tuple);
                }
                else {
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public override TupleClass read(TupleClass tuple) {
            checkFrozen();
            //Console.WriteLine("-->DEBUG:  API_SMR read");
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                readDelegate readDel = new readDelegate(serverRemoteObject.read);
                asyncResults[0] = readDel.BeginInvoke(tuple, url, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    List<TupleClass> res = readDel.EndInvoke(asyncResult);
                    return res[0];
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override TupleClass take(TupleClass tuple) {
            checkFrozen();
            //Console.WriteLine("-->DEBUG:  API_SMR take");
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                takeDelegate takeDel = new takeDelegate(serverRemoteObject.take);
                asyncResults[0] = takeDel.BeginInvoke(tuple, url, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    takeDel = (takeDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    return takeDel.EndInvoke(asyncResult)[0];
                }
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
