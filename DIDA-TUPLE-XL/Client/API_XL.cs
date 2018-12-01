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

        private string url;

        public API_XL(string URL) {
            serverRemoteObjects = prepareForRemoting(ref channel, URL);
            numServers = serverRemoteObjects.Count;

            url = URL;
        }

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate TupleClass readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeReadDelegate(TupleClass tuple, string url);
        public delegate void takeRemoveDelegate(TupleClass tuple, string url, long nonce);

        public override void Write(TupleClass tuple) {
            //Console.WriteLine("----->DEBUG_API_XL: Begin Write");
            WaitHandle[] handles = new WaitHandle[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    writeDelegate writeDel = new writeDelegate(remoteObject.Write);
                    IAsyncResult ar = writeDel.BeginInvoke(tuple, url, nonce, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                }
                if (!WaitHandle.WaitAll(handles/*TODO, 3000*/)) { //TODO check this timeout...waits for n milliseconds to receives acknoledgement of the writes, after that resends all writes
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
                int indxAsync = WaitHandle.WaitAny(handles/*TODO, 3000*/); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) { //if we have a timeout, due to no answer received with repeat the multicast TODO sera que querem isto
                    return Read(tuple);
                }
                else {//TODO se o retorno for nulo temos de ir ver outra resposta
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    readDelegate readDel = (readDelegate)((AsyncResult) asyncResult).AsyncDelegate;
                    TupleClass resTuple = readDel.EndInvoke(asyncResult);
                    nonce++;
                    return resTuple;
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override TupleClass Take(TupleClass tuple) {
            //Console.WriteLine("----->DEBUG_API_XL: Begin Take");
            //Console.Write("take in API_XL: ");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            //Console.WriteLine("----->DEBUG_API_XL: numservers " + numServers);
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    takeReadDelegate takereadDel = new takeReadDelegate(remoteObject.TakeRead);
                    IAsyncResult ar = takereadDel.BeginInvoke(tuple, url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                bool allcompleted = WaitHandle.WaitAll(handles/*TODO, 3000*/); //Wait for the first answer from the servers

                if (!allcompleted) {
                    return Take(tuple);
                }
                else { //all have completed
                    List<List<TupleClass>> responseSets = new List<List<TupleClass>>();
                    bool someRejection = false;
                    foreach (IAsyncResult asyncResult in asyncResults) {
                        takeReadDelegate takeReadDel = (takeReadDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        List<TupleClass> tupleSet = takeReadDel.EndInvoke(asyncResult);
                        if (tupleSet == null) {
                            someRejection = true;
                            break;
                        }
                        else {
                            responseSets.Add(tupleSet);
                        }
                    }
                    if (someRejection) {
                        return Take(tuple);
                    }
                    else {
                        List<TupleClass> tupleSetsIntersection = new List<TupleClass>();
                        //TODO intersect
                        //
                        //
                        //
                        if (tupleSetsIntersection.Count == 0) {
                            return Take(tuple);
                        }
                        else {
                            TupleClass tupleToDelete = tupleSetsIntersection[0];
                            //Console.WriteLine("----->DEBUG_API_XL: tuple to delete " + printTuple(tupletoDelete));
                            takeRemove(tupleToDelete);
                            nonce++;
                        }
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
            return null;
        }

        private void takeRemove(TupleClass tupleToDelete) {
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];

            for (int i = 0; i < numServers; i++) {
                IServerService remoteObject = serverRemoteObjects[i];
                takeRemoveDelegate takeRemDel = new takeRemoveDelegate(remoteObject.TakeRemove);
                IAsyncResult ar = takeRemDel.BeginInvoke(tupleToDelete, url, nonce, null, null);
                asyncResults[i] = ar;
                handles[i] = ar.AsyncWaitHandle;
                //Console.WriteLine("----->DEBUG_API_XL: asked to remove server " + i);
            }
            if (!WaitHandle.WaitAll(handles/*TODO, 3000*/)) { //TODO check this timeout...waits for n milliseconds to receives acknoledgement of the writes, after that resends all writes
                takeRemove(tupleToDelete);
            }   
        }
    }
}