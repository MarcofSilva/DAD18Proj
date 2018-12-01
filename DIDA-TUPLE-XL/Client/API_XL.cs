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
    public class API_XL : TupleSpaceAPI {

        private const int defaultPort = 8085;
        private TcpChannel channel;
        private List<IServerService> serverRemoteObjects;
        private int numServers;
        private bool frozen = false;

        private string url;

        public API_XL(string URL) {
            serverRemoteObjects = prepareForRemoting(ref channel, URL);
            numServers = serverRemoteObjects.Count;

            url = URL;
        }

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeReadDelegate(TupleClass tuple, string url, long nonce);
        public delegate void takeRemoveDelegate(TupleClass tuple, string url, long nonce);

        public override void Write(TupleClass tuple) {
            //Console.WriteLine("----->DEBUG_API_XL: Begin Write");
            checkFrozen();
            WaitHandle[] handles = new WaitHandle[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    writeDelegate writeDel = new writeDelegate(remoteObject.Write);
                    IAsyncResult ar = writeDel.BeginInvoke(tuple, url, nonce, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                }
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    Write(tuple);
                }
                else {
                    nonce += 1;
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override TupleClass Read(TupleClass tuple) {
            checkFrozen();
            //Console.WriteLine("----->DEBUG_API_XL alkjsdkajsd: Begin Read");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    readDelegate readDel = new readDelegate(remoteObject.Read);
                    IAsyncResult ar = readDel.BeginInvoke(tuple, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int indxAsync = WaitHandle.WaitAny(handles, 3000); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) { //if we have a timeout, due to no answer received with repeat the multicast TODO sera que querem isto
                    return Read(tuple);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    readDelegate readDel = (readDelegate)((AsyncResult) asyncResult).AsyncDelegate;
                    List<TupleClass> resTuple = readDel.EndInvoke(asyncResult);
                    nonce += 1;
                    if (resTuple.Count == 0) {
                        Console.WriteLine("--->DEBUG: No tuple returned from server");
                        return new TupleClass();
                    }
                    return resTuple[0];
                }
            }
            catch (SocketException e) {
                //TODO
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        public override TupleClass Take(TupleClass tuple) {
            checkFrozen();
            //Console.WriteLine("----->DEBUG_API_XL: Begin Take");
            //Console.Write("take in API_XL: ");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            //Console.WriteLine("----->DEBUG_API_XL: numservers " + numServers);
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    takeReadDelegate takereadDel = new takeReadDelegate(remoteObject.TakeRead);
                    IAsyncResult ar = takereadDel.BeginInvoke(tuple, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                bool allcompleted = WaitHandle.WaitAll(handles, 3000); //Wait for the first answer from the servers
                List<TupleClass> res = new List<TupleClass>();
                List<TupleClass> intersect = new List<TupleClass>();
                if (!allcompleted) {
                    return Take(tuple);
                }
                else{ //all have to completed
                    for (int i = 0; i < numServers; i++) {
                        //Console.WriteLine("----->DEBUG_API_XL: iteration " + i);
                        IAsyncResult asyncResult = asyncResults[i];
                        takeReadDelegate takeReadDel = (takeReadDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        List<TupleClass> resTuple = takeReadDel.EndInvoke(asyncResult);
                        nonce += 1;
                        if (resTuple.Count == 0) {
                            //Console.WriteLine("--->DEBUG: Interception is empty, no tuples to remove");
                            return new TupleClass();
                        }
                        if (i == 0) {
                            res = resTuple;
                            //Console.WriteLine("----->DEBUG_API_XL: ITERATION ONE SIZE:" + res.Count);
                        }
                        else {
                            bool remove = true;
                            foreach(TupleClass inter in res) {
                                remove = true;
                                foreach (TupleClass el in resTuple) {
                                    if (inter.Equals(el)) {
                                        remove = false;
                                    }
                                }
                                if (remove) {
                                    //pode dar probs porque estamos a alterar uma lista a ser iterada
                                    res.Remove(inter);
                                }
                            }
                            if (res.Count == 0) {
                                //intersection is empty and will always be empty
                                //Console.WriteLine("--->DEBUG: Interception is empty, no tuples to remove");
                                return new TupleClass();
                            }
                            //Console.WriteLine("----->DEBUG_API_XL: intersect size " + res.Count);
                            //Console.WriteLine("----->DEBUG_API_XL: intersect " + printTuple(res[0]));
                        }
                    }
                }
                //chose first commun to all?
                if(res.Count == 0) {
                    //Console.WriteLine("--->DEBUG: Interception is empty, no tuples to remove");
                    return new TupleClass();
                }
                TupleClass tupletoDelete = res[0];
                //Console.WriteLine("----->DEBUG_API_XL: tuple to delete " + printTuple(tupletoDelete));
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    takeRemoveDelegate takeremDel = new takeRemoveDelegate(remoteObject.TakeRemove);
                    IAsyncResult ar = takeremDel.BeginInvoke(tupletoDelete, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    //Console.WriteLine("----->DEBUG_API_XL: asked to remove server " + i);
                }
                //should we just wait for all or certify they return ack?
                allcompleted = WaitHandle.WaitAll(handles, 3000); //Wait for the first answer from the servers
                if (!allcompleted) {
                    return Take(tuple);
                }
                nonce += 1;
                return tupletoDelete;
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
            return null;//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
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