using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    class FailureDetector {

        private List<string> allServers = new List<string>();
        private List<string> view = new List<string>();
        private int numServers;
        private Dictionary<string, IServerService> serverRemoteObjects = new Dictionary<string, IServerService>();
        public delegate int pingDelegate();
        private string _url;

        public FailureDetector(string serverUrl) {
            configure();
            _url = serverUrl;
            Thread t = new Thread(() => pingLoop());
            t.Start();
        }

        public void pingLoop() {
            while (true) {
                List<string> oldView = view;
                WaitHandle[] handles = new WaitHandle[numServers];
                IAsyncResult[] asyncResults = new IAsyncResult[numServers];
                try {
                    int i = 0;
                    int[] responses = new int[numServers];
                    foreach (KeyValuePair<string, IServerService> remoteObjectpair in serverRemoteObjects) {
                        ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                        pingDelegate pingDel = new pingDelegate(remoteObject.Ping);
                        IAsyncResult ar = pingDel.BeginInvoke(null, null);
                        
                        asyncResults[i] = ar;
                        handles[i] = ar.AsyncWaitHandle;
                        i++;
                    }
                    if (!WaitHandle.WaitAll(handles, 300)) {
                        //Console.WriteLine("TIMEOUT");
                        for (int k = 0; k < numServers; k++) {
                            //Console.WriteLine(handles[k].WaitOne(0));
                            if(handles[k].WaitOne(0) == false) {
                                responses[k] = -1;
                            } 
                        }
                    }
                    for (i = 0; i < numServers; i++) {
                        try {
                            if (responses[i] != -1) { //responses with -1 already timed out, we don't want to endinvoke them
                                IAsyncResult asyncResult = asyncResults[i];
                                pingDelegate pingDel = (pingDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                                responses[i] = pingDel.EndInvoke(asyncResult);
                            }
                        }
                        catch (SocketException e) {
                            responses[i] = -1;
                        }
                        catch (NullReferenceException e) {
                            responses[i] = -1;
                        }
                    }
                    lock (view) {
                        view = new List<string>();
                        for (int j = 0; j < numServers; j++) {
                            if (responses[j] != -1) {
                                view.Add(allServers[j]);
                            }
                            else {
                                //Console.WriteLine(j.ToString() + " is down");
                            }
                        }
                        //Console.WriteLine("view count: " + view.Count);
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                }

                bool isChanged = false;
                if (oldView.Count != view.Count) isChanged = true;
                
                foreach (string bla in view) {
                    //Console.WriteLine("-> " + bla);
                    if (!oldView.Contains(bla)) {
                        isChanged = true;
                    }
                }
                if (isChanged) {
                    Console.WriteLine("view changed!!!");
                }
            }
        }

        public List<string> getView() {
            //Console.WriteLine("view request - count: " + view.Count());
            foreach (string bla in view) {
                //Console.WriteLine("-->" + bla);
            }
            return view;
        }

        public delegate List<TupleClass> askUpdateDelegate();
        public List<TupleClass> updateTS() {
            while(view.Count == 0) {
                Thread.Sleep(100);
            }
            WaitHandle[] handles = new WaitHandle[view.Count-1];
            IAsyncResult[] asyncResults = new IAsyncResult[view.Count-1];
            try {
                int i = 0;
                List<TupleClass> result = new List<TupleClass>();
                foreach (string url in view) {
                    if (url == _url)
                        continue;
                    ServerService remoteObject = (ServerService)serverRemoteObjects[url];
                    askUpdateDelegate askUpdateDel = new askUpdateDelegate(remoteObject.askUpdate);
                    IAsyncResult ar = askUpdateDel.BeginInvoke(null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return updateTS(); //TODO
                }
                List<TupleClass> localRes = new List<TupleClass>();
                for (i = 0; i < view.Count-1; i++) {
                    try {
                        IAsyncResult asyncResult = asyncResults[i];
                        askUpdateDelegate askUpdateDel = (askUpdateDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        localRes = askUpdateDel.EndInvoke(asyncResult);
                    }
                    catch (SocketException e) {
                    }
                    if (i == 0) {
                        result = localRes;
                    }
                    else {
                        if (!compareList(result, localRes)) {
                            Thread.Sleep(300);//if the servers we asked are not the same, we need to wait for them to sync
                            return updateTS();
                        }
                    }
                }
                return result;
            }
            catch (Exception e) {
                Console.WriteLine(e.StackTrace);
                return null;
            }
        }

        private bool compareList(List<TupleClass> l1, List<TupleClass> l2) {
            if (l1.Count != l2.Count) {
                return false;
            }
            else {
                for(int i = 0; i < l1.Count; i++) {
                    if (!l1[i].Equals( l2[i])) {
                        return false;
                    }
                }
                return true;
            }
        }

        public void configure() {
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                allServers.Add(url);
                IServerService obj = (IServerService)Activator.GetObject(typeof(IServerService), url);
                serverRemoteObjects.Add(url, obj);
            }
            numServers = serverRemoteObjects.Count();
        }

       
    }
}
