using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server {
    class FailureDetector {

        private static List<string> allServers = new List<String>() { "tcp://localhost:8086/S", "tcp://localhost:8087/S", "tcp://localhost:8088/S" }; //TODO num ficheiro a parte
        private List<string> view = new List<string>();
        private Dictionary<string, IServerService> serverRemoteObjects = new Dictionary<string, IServerService>();
        public delegate int pingDelegate();

        public FailureDetector() {
            configure();
            Thread t = new Thread(() => pingLoop());
            t.Start();
        }

        public void pingLoop() {
            while (true) {
                List<string> oldView = view;
                WaitHandle[] handles = new WaitHandle[allServers.Count];
                IAsyncResult[] asyncResults = new IAsyncResult[allServers.Count];
                try {
                    int i = 0;
                    int[] responses = new int[allServers.Count];
                    foreach (KeyValuePair<string, IServerService> remoteObjectpair in serverRemoteObjects) {
                        ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                        pingDelegate pingDel = new pingDelegate(remoteObject.Ping);
                        IAsyncResult ar = pingDel.BeginInvoke(null, null);
                        
                        asyncResults[i] = ar;
                        handles[i] = ar.AsyncWaitHandle;
                        i++;
                    }
                    if (!WaitHandle.WaitAll(handles, 300)) {
                        Console.WriteLine("TIMEOUT");
                        for (int k = 0; k < allServers.Count; k++) {
                            Console.WriteLine(handles[k].WaitOne(0));
                            if(handles[k].WaitOne(0) == false) {
                                responses[k] = -1;
                            } 
                        }
                    }
                    else {
                        for (i = 0; i < allServers.Count; i++) {
                            try {
                                IAsyncResult asyncResult = asyncResults[i];
                                pingDelegate pingDel = (pingDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                                responses[i] = pingDel.EndInvoke(asyncResult);
                                //Console.WriteLine(response);
                            }
                            catch (SocketException e) {
                                responses[i] = -1;
                            }
                            catch (NullReferenceException e) {
                                responses[i] = -1;
                            }
                        }
                    }
                    lock (view) {
                        view = new List<string>();
                        for (int j = 0; j < allServers.Count; j++) {
                            if (responses[j] != -1) {
                                view.Add(allServers[j]);
                            }
                        }
                    }
                    Console.WriteLine("view count: " + view.Count);
                }
                catch (Exception e) {
                    Console.WriteLine(e.StackTrace);
                }

                bool isChanged = false;
                if (oldView.Count != view.Count) isChanged = true;
                
                foreach (string bla in view) {
                    Console.WriteLine("-> " + bla);
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
            Console.WriteLine("view request - count: " + view.Count());
            foreach (string bla in view) {
                Console.WriteLine("-->" + bla);
            }
            return view;
        }

        public void configure() {
            foreach (string url in allServers) {
                IServerService obj = (IServerService)Activator.GetObject(typeof(IServerService), url);
                serverRemoteObjects.Add(url, obj);
            }
        }

       
    }
}
