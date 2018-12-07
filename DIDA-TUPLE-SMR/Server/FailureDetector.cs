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
    public class FailureDetector {

        private List<string> allServers = new List<string>();
        private List<string> view = new List<string>();
        private int numServers;
        private Dictionary<string, IServerService> serverRemoteObjects = new Dictionary<string, IServerService>();
        public delegate int pingDelegate();

        private bool modified = true;
        

        public FailureDetector() {
            configure();
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
                        for (int k = 0; k < numServers; k++) {
                            if (handles[k].WaitOne(0) == false) {
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
                        catch (SocketException ) {
                            responses[i] = -1;
                        }
                        catch (NullReferenceException ) {
                            responses[i] = -1;
                        }
                    }
                    lock (view) {
                        view = new List<string>();
                        for (int j = 0; j < numServers; j++) {
                            if (responses[j] != -1) {
                                view.Add(allServers[j]);
                            }
                        }
                    }
                }
                catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                }

                bool isChanged = false;
                if (oldView.Count != view.Count) isChanged = true;

                foreach (string bla in view) {
                    if (!oldView.Contains(bla)) {
                        isChanged = true;
                    }
                }
                if (isChanged) {
                    modified = true;
                    Console.WriteLine("View Changed. View count: " + view.Count);
                }
            }
        }

        public List<string> getViewNormal() {
            return view;
        }

        public List<string> getView() {
            modified = false;
            return view;
        }

        public bool changed() {
            return modified;
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
