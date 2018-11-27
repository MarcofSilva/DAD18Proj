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

        public delegate void writeDelegate(ArrayList tuple, string url, long nonce);
        public delegate List<ArrayList> readDelegate(ArrayList tuple, string url, long nonce);
        public delegate List<ArrayList> takeReadDelegate(ArrayList tuple, string url, long nonce);
        public delegate void takeRemoveDelegate(ArrayList tuple, string url, long nonce);

        public override void Write(ArrayList tuple) {
            WaitHandle[] handles = new WaitHandle[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    writeDelegate writeDel = new writeDelegate(remoteObject.Write);
                    IAsyncResult ar = writeDel.BeginInvoke(tuple, url, nonce, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                }
                if (!WaitHandle.WaitAll(handles, 1000)) {
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

        public override ArrayList Read(ArrayList tuple) {
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
                    List<ArrayList> resTuple = readDel.EndInvoke(asyncResult);
                    nonce += 1;
                    if (resTuple.Count == 0) {
                        Console.WriteLine("--->DEBUG: No tuple returned from server");
                        return new ArrayList();
                    }
                    return resTuple[0];
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override ArrayList Take(ArrayList tuple) {
            //Console.Write("take in API_XL: ");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    takeReadDelegate takereadDel = new takeReadDelegate(remoteObject.TakeRead);
                    IAsyncResult ar = takereadDel.BeginInvoke(tuple, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                bool allcompleted = WaitHandle.WaitAll(handles, 3000); //Wait for the first answer from the servers
                List<ArrayList> res = new List<ArrayList>();
                if (!allcompleted) {
                    return Take(tuple);
                }
                else{ //all have to completed
                    for (int i = 0; i < numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        takeReadDelegate takeReadDel = (takeReadDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        List<ArrayList> resTuple = takeReadDel.EndInvoke(asyncResult);
                        //TODO
                        if (i == 0) {
                            res = resTuple;
                        }
                        else {
                            //TODO comparador pode estar mal, 2 tuplos iguais podem nao dar igual no intersect
                            //testar 2 servidores e ver se o mesmo tuplo e considerado uma intersecao
                            res = res.Intersect(resTuple).ToList();
                        }
                    }
                }
                //chose first commun to all?
                if(res.Count == 0) {
                    //Console.WriteLine("--->DEBUG: Interception is empty, no tuples to remove");
                    return new ArrayList();
                }
                ArrayList tupletoDelete = res[0];
                nonce += 1;
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = serverRemoteObjects[i];
                    takeRemoveDelegate takeremDel = new takeRemoveDelegate(remoteObject.TakeRemove);
                    IAsyncResult ar = takeremDel.BeginInvoke(tupletoDelete, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
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
    }
}