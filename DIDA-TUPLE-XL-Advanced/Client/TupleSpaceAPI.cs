using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using ClassLibrary;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Remoting.Messaging;
using System.Net.Sockets;

namespace Client {
    public abstract class TupleSpaceAPI {
        private ClientService myRemoteObject;
        private List<IServerService> serverRemoteObjects = new List<IServerService>();

        private long _operationNonce;

        public TupleSpaceAPI() {
            _operationNonce = 0;
        }

        public long nonce {
            get {
                return _operationNonce;
            }

            set {
                _operationNonce = value;
            }
        }

        public abstract void Write(TupleClass tuple);

        public abstract TupleClass Read(TupleClass tuple);

        public abstract TupleClass Take(TupleClass tuple);

        public abstract void freeze();

        public abstract void unfreeze();

        public delegate List<string> requestViewDelegate();

        protected List<IServerService> prepareForRemoting(ref TcpChannel channel, string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Int32.TryParse(urlSplit[2], out port);
            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ClientService(this);
            RemotingServices.Marshal(myRemoteObject, urlSplit[3], typeof(ClientService));
            Console.WriteLine("Hello! I'm a Client at port " + urlSplit[2]);
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }
            return serverRemoteObjects;
        }

        private List<string> union(List<string> l1, List<string> l2) {
            List<string> res = new List<string>();
            res = l1.Union(l2).ToList();
            return res;
        }

        public List<IServerService> getView(List<IServerService> view) {
            int numServers;
            if (view.Count == 0) {
                Console.WriteLine("No connections found. Broadcasting...");
                view = serverRemoteObjects;
                numServers = ConfigurationManager.AppSettings.AllKeys.Count();
            }
            else {
                numServers = view.Count;
            }
            WaitHandle[] handles = new WaitHandle[numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[numServers];
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    requestViewDelegate viewDel = new requestViewDelegate(remoteObject.ViewRequest);
                    IAsyncResult ar = viewDel.BeginInvoke(null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int[] timeouts = new int[numServers];
                if (!WaitHandle.WaitAll(handles, 1000)) {
                    for (int k = 0; k < numServers; k++) {
                        if (handles[k].WaitOne(0) == false) {
                            timeouts[k]++;
                        }
                    }
                }
                if (timeouts.Sum() > numServers / 2) {
                    //Majority of timeouts
                    getView(view);
                }
                
                else {
                    List<string> viewUnion = new List<string>();
                    int i = 0;
                    for(; i < numServers; i++) { //dont invoke timeouts
                        if (timeouts[i] != 1) {
                            try {
                                requestViewDelegate viewDel = (requestViewDelegate)((AsyncResult)asyncResults[i]).AsyncDelegate;
                                List<string> servers = viewDel.EndInvoke(asyncResults[i]);
                                viewUnion = union(servers, viewUnion);
                            }
                            catch (SocketException) {
                                List<IServerService> newView = view;
                                //If there is a socket exception, it is because he is down
                                //Therefore we remove it from view
                                newView.RemoveAt(i);
                                return getView(newView);
                            }
                        }
                    }
                    if (viewUnion.Count() != 0) {
                        List<IServerService> serverobjs = new List<IServerService>();
                        for (int j = 0; j < viewUnion.Count; j++) {
                            serverobjs.Add((IServerService)Activator.GetObject(typeof(IServerService), viewUnion[j]));
                        }
                        return serverobjs;
                    }
                    else {
                        //View is empty, try again
                        return getView(view);
                    }

                }
            }
            catch (SocketException) {
                Console.WriteLine("Connection error. Restarting...");
                return getView(serverRemoteObjects);
            }
            return getView(serverRemoteObjects);
        }
    }
}
