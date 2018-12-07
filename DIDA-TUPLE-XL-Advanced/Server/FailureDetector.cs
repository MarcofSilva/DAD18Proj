using ClassLibrary;
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

namespace Server {
    class FailureDetector {

        private List<string> allServers = new List<string>();
        private List<string> view = new List<string>();
        private List<string> suspects = new List<string>();
        private int numServers;
        private Dictionary<string, IServerService> serverRemoteObjects = new Dictionary<string, IServerService>();
        private int[] timeouts;
        private Server _server;
        public delegate int pingDelegate();
        private string _url;

        public FailureDetector(Server server) {
            _server = server;
            _url = server.url;
            configure();
            Thread t = new Thread(() => pingLoop());
            t.Start();
        }

        public void pingLoop() {
            while (true) {
                if (!_server.frozen) {
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
                            for (int k = 0; k < numServers; k++) {
                                if (handles[k].WaitOne(timeouts[k]) == false) {
                                    responses[k] = -2;
                                    if (!suspects.Contains(allServers[k])) { //timeout e ainda não era suspeito
                                        Console.WriteLine("Added " + allServers[k] + " to suspects list");
                                        suspects.Add(allServers[k]);
                                    }
                                }
                            }
                        }
                        for (i = 0; i < numServers; i++) {
                            try {
                                if (responses[i] != -2) { //responses with -2 already timed out, we don't want to endinvoke them
                                    IAsyncResult asyncResult = asyncResults[i];
                                    pingDelegate pingDel = (pingDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                                    responses[i] = pingDel.EndInvoke(asyncResult);
                                }
                            }
                            catch (SocketException e) {
                                responses[i] = -1;  // -1 means they are dead
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
                                if (responses[j] != -1 && responses[j] != -2 && suspects.Contains(allServers[j])) {
                                    suspects.Remove(allServers[j]);
                                    timeouts[j] += 100;
                                    Console.WriteLine("Increased timeout (" + allServers[j] + " - " + timeouts[j].ToString() + ")");
                                }
                            }
                        }

                    }
                    catch (Exception e) {
                        Console.WriteLine(e.StackTrace);
                    }
                    bool isChanged = false;
                    if (oldView.Count != view.Count) isChanged = true;

                    foreach (string str in view) {
                        if (!oldView.Contains(str)) {
                            isChanged = true;
                        }
                    }
                    if (isChanged) {
                        Console.WriteLine("View Changed");
                    }
                }
                else {
                    Thread.Sleep(100);
                }
            }
        }

        public delegate List<TupleClass> askUpdateDelegate();
        public List<TupleClass> updateTS() {
            while (view.Count == 0) {
                Thread.Sleep(100);
            }
            WaitHandle[] handles = new WaitHandle[view.Count - 1];
            IAsyncResult[] asyncResults = new IAsyncResult[view.Count - 1];
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
            for (i = 0; i < view.Count - 1; i++) {
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
                        Thread.Sleep(300);//If servers do not agree with the tuple space, we need to let them sync
                        return updateTS();
                    }
                }
            }
            return result;
        }

        private bool compareList(List<TupleClass> l1, List<TupleClass> l2) {
            if (l1.Count != l2.Count) {
                return false;
            }
            else {
                for (int i = 0; i < l1.Count; i++) {
                    if (!l1[i].Equals(l2[i])) {
                        return false;
                    }
                }
                return true;
            }
        }

        public List<string> getView() {
            return view;
        }

        public List<string> getSuspects() {
            return suspects;
        }

        public void configure() {
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                allServers.Add(url);
                IServerService obj = (IServerService)Activator.GetObject(typeof(IServerService), url);
                serverRemoteObjects.Add(url, obj);
            }
            numServers = serverRemoteObjects.Count();
            timeouts = new int[numServers]; //all 0;
        }


    }
}
