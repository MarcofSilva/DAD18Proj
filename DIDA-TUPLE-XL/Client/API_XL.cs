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

        private int numServers;
        private bool frozen = false;
        private string url;
        private List<IServerService> view = new List<IServerService>();

        public API_XL(string URL) {
            url = URL;
            prepareForRemoting(ref channel, URL);
            Console.WriteLine("Requesting available servers...");
            setView();
            Console.WriteLine("Done!");
        }

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate TupleClass readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeReadDelegate(TupleClass tuple, string url);
        public delegate void takeRemoveDelegate(TupleClass tuple, string url, long nonce);

        public override void Write(TupleClass tuple) {
            
            checkFrozen();
            setView();
            Console.WriteLine("----->DEBUG_API_XL: Begin Write");
            WaitHandle[] handles = new WaitHandle[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    writeDelegate writeDel = new writeDelegate(remoteObject.Write);
                    IAsyncResult ar = writeDel.BeginInvoke(tuple, url, nonce, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                }
                if (!WaitHandle.WaitAll(handles, 3000)) { //TODO check this timeout...waits for n milliseconds to receives acknoledgement of the writes, after that resends all writes
                    Write(tuple);
                }
                else {
                    nonce ++;
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override TupleClass Read(TupleClass tuple) {
            checkFrozen();
            setView();
            Console.WriteLine("----->DEBUG_API_XL alkjsdkajsd: Begin Read");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    readDelegate readDel = new readDelegate(remoteObject.Read);
                    IAsyncResult ar = readDel.BeginInvoke(tuple, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int indxAsync = WaitHandle.WaitAny(handles, 3000); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) { //if we have a timeout, due to no answer received with repeat the multicast TODO sera que querem isto
                    return Read(tuple);
                }
                else {//TODO se o retorno for nulo temos de ir ver outra resposta
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    readDelegate readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    TupleClass resTuple = readDel.EndInvoke(asyncResult); //TODO ou mudar no smr receber tuple ou aqui para receber list
                    nonce++;
                    return resTuple;
                }
            }
            catch (SocketException e) {
                //TODO
                Console.WriteLine("Error in read. Trying again...");
                return Read(tuple);
            }
        }

        public override TupleClass Take(TupleClass tuple) {
            checkFrozen();
            setView();
            Console.WriteLine("----->DEBUG_API_XL: Begin Take");
            //Console.Write("take in API_XL: ");
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            //Console.WriteLine("----->DEBUG_API_XL: numservers " + numServers);
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    takeReadDelegate takereadDel = new takeReadDelegate(remoteObject.TakeRead);
                    IAsyncResult ar = takereadDel.BeginInvoke(tuple, url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                bool allcompleted = WaitHandle.WaitAll(handles, 3000); //Wait for the first answer from the servers

                if (!allcompleted) {
                    Console.WriteLine("timeout");
                    return Take(tuple);
                }
                else { //all have completed
                    List<TupleClass> response = new List<TupleClass>();
                    bool firstiteration = true;
                    
                    foreach (IAsyncResult asyncResult in asyncResults) {
                        takeReadDelegate takeReadDel = (takeReadDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        List<TupleClass> tupleSet = takeReadDel.EndInvoke(asyncResult);
                        if (firstiteration) {
                            firstiteration = false;
                            response = tupleSet;
                        }
                        else {
                            response = listIntersection(response, tupleSet);
                            if (response.Count == 0) {
                                Console.WriteLine("No possible intersection. Repeating...");
                                return Take(tuple);
                            }
                        }
                    }
                    TupleClass tupleToDelete = response[0];
                    //Console.WriteLine("----->DEBUG_API_XL: tuple to delete " + printTuple(tupletoDelete));
                    takeRemove(tupleToDelete);
                    nonce++;
                    return tupleToDelete; 
                }
            }
            catch (SocketException) {
                Console.WriteLine("Error in take. Trying again...");
                return Take(tuple);
            }
        }

        private void takeRemove(TupleClass tupleToDelete) {
            setView();
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];

            for (int i = 0; i < numServers; i++) {
                IServerService remoteObject = view[i];
                takeRemoveDelegate takeRemDel = new takeRemoveDelegate(remoteObject.TakeRemove);
                IAsyncResult ar = takeRemDel.BeginInvoke(tupleToDelete, url, nonce, null, null);
                asyncResults[i] = ar;
                handles[i] = ar.AsyncWaitHandle;
                //Console.WriteLine("----->DEBUG_API_XL: asked to remove server " + i);
            }
            if (!WaitHandle.WaitAll(handles, 3000)) { //TODO check this timeout...waits for n milliseconds to receives acknoledgement of the writes, after that resends all writes
                takeRemove(tupleToDelete);
            }
        }

        public override void freeze() {
            frozen = true;
        }

        
        private List<TupleClass> listIntersection(List<TupleClass> tl1, List<TupleClass> tl2) {
            int i;
            bool remove;
            foreach (TupleClass t1 in tl1) {
                remove = true;
                foreach (TupleClass t2 in tl2) {
                    if (t1.Equals(t2)) {
                        remove = false;
                        break;
                    }
                }
                if (remove) {
                    tl1.Remove(t1);
                }
            }
            return tl1;
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

        public void setView() {
            if (view == null)
                view = new List<IServerService>();
            view = getView(view);
            //if (view == null || view.Count == 0) setView();
            numServers = view.Count;
            Console.WriteLine("got view");

        }
    }
}