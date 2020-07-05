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
        private Random random = new Random();

        public API_XL(string URL) {
            url = URL;
            prepareForRemoting(ref channel, URL);
            setView();
            Console.WriteLine("Finished requesting servers!");
        }

        public delegate void writeDelegate(TupleClass tuple, string url, long nonce);
        public delegate TupleClass readDelegate(TupleClass tuple, string url, long nonce);
        public delegate List<TupleClass> takeReadDelegate(TupleClass tuple, string url);
        public delegate void takeRemoveDelegate(TupleClass tuple, string url, long nonce);
        public delegate void releaseLocksDelegate(string url);


        public override void Write(TupleClass tuple) {
            checkFrozen();
            WaitHandle[] handles = new WaitHandle[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    writeDelegate writeDel = new writeDelegate(remoteObject.Write);
                    IAsyncResult ar = writeDel.BeginInvoke(tuple, url, nonce, null, null);
                    handles[i] = ar.AsyncWaitHandle;
                }
                int ntimeouts = 0;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    for (int k = 0; k < numServers; k++) {
                        if (handles[k].WaitOne(0) == false) {
                            ntimeouts++;
                        }
                    }
                }
                if (ntimeouts > numServers / 2) {
                    //Majority of timeouts
                    setView();
                    Write(tuple);
                }
                else {
                    nonce++;
                }
            }
            catch (SocketException) {
                Console.WriteLine("Error in write. Trying again...");
                setView();
                Write(tuple);
            }
        }

        public override TupleClass Read(TupleClass tuple) {
            checkFrozen();
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    readDelegate readDel = new readDelegate(remoteObject.Read);
                    IAsyncResult ar = readDel.BeginInvoke(tuple, url, nonce, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int indxAsync = WaitHandle.WaitAny(handles, 1000);
                if (indxAsync == WaitHandle.WaitTimeout) {
                    Thread.Sleep(200);
                    setView();
                    return Read(tuple);
                }
                else {//TODO se o retorno for nulo temos de ir ver outra resposta
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    readDelegate readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    TupleClass resTuple = readDel.EndInvoke(asyncResult);
                    nonce++;
                    return resTuple;
                }
            }
            catch (SocketException) {
                Console.WriteLine("Error in read. Trying again...");
                setView();
                return Read(tuple);
            }
        }

        public override TupleClass Take(TupleClass tuple) {
            checkFrozen();
            
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            int nAccepts = 0;
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    takeReadDelegate takereadDel = new takeReadDelegate(remoteObject.TakeRead);
                    IAsyncResult ar = takereadDel.BeginInvoke(tuple, url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                //Wait for the first answer from the servers
                bool allcompleted = WaitHandle.WaitAll(handles, random.Next(1000, 2000));
                int[] ntimeouts = new int[numServers];
                if (!allcompleted) {
                    for (int k = 0; k < numServers; k++) {
                        if (handles[k].WaitOne(0) == false) {
                            ntimeouts[k]++;
                        }
                    }
                }
                if (ntimeouts.Sum() > numServers / 2) {
                    //Majority of timeouts
                    Thread.Sleep(200);
                    setView();
                    return Take(tuple);
                }
                else {
                    List<List<TupleClass>> responses = new List<List<TupleClass>>();
                    for (int j = 0; j < numServers; j++) {
                        if (ntimeouts[j] != 1) {
                            takeReadDelegate takeReadDel = (takeReadDelegate)((AsyncResult)asyncResults[j]).AsyncDelegate;
                            List<TupleClass> res = takeReadDel.EndInvoke(asyncResults[j]);
                            responses.Add(res);
                            if (res.Count != 0)
                                nAccepts++;
                        }
                    }
                    int realServers = numServers - ntimeouts.Sum();
                    if (nAccepts != realServers && nAccepts > realServers / 2) {
                        //Majority of accepts
                        Thread.Sleep(200);
                        setView();
                        return Take(tuple);
                    }
                    else if (nAccepts <= realServers / 2 && realServers != 1) {
                        //Minority of accepts
                        for (int i = 0; i < realServers; i++) {
                            IServerService remoteObject = view[i];
                            releaseLocksDelegate releaseLocksDelegate = new releaseLocksDelegate(remoteObject.releaseLocks);
                            IAsyncResult ar = releaseLocksDelegate.BeginInvoke(url, null, null);
                            asyncResults[i] = ar;
                            handles[i] = ar.AsyncWaitHandle;
                        }
                        Thread.Sleep(200);
                        setView();
                        return Take(tuple);
                    }
                    else {
                        List<TupleClass> response = new List<TupleClass>();
                        bool firstiteration = true;
                        foreach (List<TupleClass> list in responses) {
                            if (firstiteration) {
                                firstiteration = false;
                                response = list;
                            }
                            else {
                                response = listIntersection(response, list);
                                if (response.Count == 0) {
                                    //In case where intersection of all takeRead responses is empty
                                    Thread.Sleep(200);
                                    return (tuple);
                                }
                            }

                        }
                        TupleClass tupleToDelete = response[0];
                        takeRemove(tupleToDelete);
                        nonce++;
                        return tupleToDelete;
                    }
                }
            }
            catch (SocketException) {
                Console.WriteLine("Error in take. Trying again...");
                setView();
                return Take(tuple);
            }
        }

        private void takeRemove(TupleClass tupleToDelete) {
            
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];

            for (int i = 0; i < numServers; i++) {
                IServerService remoteObject = view[i];
                takeRemoveDelegate takeRemDel = new takeRemoveDelegate(remoteObject.TakeRemove);
                IAsyncResult ar = takeRemDel.BeginInvoke(tupleToDelete, url, nonce, null, null);
                asyncResults[i] = ar;
                handles[i] = ar.AsyncWaitHandle;
            }
            int ntimeouts = 0;
            if (!WaitHandle.WaitAll(handles, 1000)) {
                for (int k = 0; k < numServers; k++) {
                    if (handles[k].WaitOne(0) == false) {
                        ntimeouts++;
                    }
                }
            }
            if (ntimeouts > numServers / 2) {
                setView();
                takeRemove(tupleToDelete);
            }
        }

        public override void freeze() {
            frozen = true;
        }

        private List<TupleClass> listIntersection(List<TupleClass> tl1, List<TupleClass> tl2) {
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
                Console.WriteLine("Can't do anything, I'mm frozen");
                lock (this) {
                    while (frozen) {
                        Monitor.Wait(this);
                    }
                }
            }
        }

        public void setView() {
            if (view == null)
                view = new List<IServerService>();
            view = getView(view);
            numServers = view.Count;
        }
    }
}