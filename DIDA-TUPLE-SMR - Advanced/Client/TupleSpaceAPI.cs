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
using System.Threading.Tasks;
using ClassLibrary;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.Remoting.Messaging;

namespace Client {
    public abstract class TupleSpaceAPI {

        private long _operationNonce;
        private List<IServerService> serverRemoteObjects = new List<IServerService>();

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

        public abstract void write(TupleClass tuple);

        public abstract TupleClass read(TupleClass tuple);

        public abstract TupleClass take(TupleClass tuple);

        public abstract void freeze();

        public abstract void unfreeze();

        public delegate List<string> requestViewDelegate();

        protected IServerService prepareForRemoting(ref TcpChannel channel, string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Console.WriteLine(URL);

            Int32.TryParse(urlSplit[2], out port);

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);

            Console.WriteLine("Hello! I'm a Client at port " + urlSplit[2]);

            //TODO nao usar allkeys (problema se este servidor em especifico nao tiver sido iniciado
            string url = ConfigurationManager.AppSettings.AllKeys[0];

            IServerService serverRemoteObject = ((IServerService)Activator.GetObject(typeof(IServerService), url));

            foreach (string serverUrl in ConfigurationManager.AppSettings.AllKeys) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), serverUrl));
            }
            return serverRemoteObject;
        }

        public List<IServerService> getView(List<IServerService> view) {
            int numServers;
            if (view.Count == 0) {
                Console.WriteLine("No connections found. Broadcasting...");
                view = serverRemoteObjects;
                numServers = ConfigurationManager.AppSettings.AllKeys.Count(); //TODO use same file for client and server!!
            }
            else {
                numServers = view.Count;
            }
            WaitHandle[] handles = new WaitHandle[numServers];
            Console.WriteLine("Broadcasting to " + numServers + " servers...");
            IAsyncResult[] asyncResults = new IAsyncResult[numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    requestViewDelegate viewDel = new requestViewDelegate(remoteObject.ViewRequest);
                    Console.WriteLine(i);
                    IAsyncResult ar = viewDel.BeginInvoke(null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int indxAsync = WaitHandle.WaitAny(handles, 300); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) {
                    Console.WriteLine("timeout with " + numServers.ToString());
                    getView(view);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    requestViewDelegate viewDel = (requestViewDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    try {
                        List<string> servers = viewDel.EndInvoke(asyncResult);
                        if (servers.Count() != 0) {
                            List<IServerService> serverobjs = new List<IServerService>();
                            for (int j = 0; j < servers.Count; j++) {
                                Console.WriteLine("answer from " + indxAsync + ": " + servers[j] + "(" + servers.Count + ")");
                                serverobjs.Add((IServerService)Activator.GetObject(typeof(IServerService), servers[j]));
                            }
                            return serverobjs;
                        }
                        else {
                            return getView(view); //TODO? I dont like this
                        }
                    }
                    catch (SocketException) {
                        Console.WriteLine("ERROR: view is " + view.Count());
                        Console.WriteLine("Server " + indxAsync + " is down. Restarting...");
                        List<IServerService> newView = view;

                        newView.RemoveAt(indxAsync);
                        Console.WriteLine("Trying again with " + newView.Count);
                        return getView(newView);
                    }

                }
            }
            catch (SocketException e) {
                Console.WriteLine("Connection error. Restarting...");
                return getView(serverRemoteObjects); //TODO? isto acho que nao precisa de estar aqui (este catch)
            }
            Console.WriteLine("you shouldnt be here");
            return null;
        }
    }
}

/*
 
    
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

        public List<IServerService> getView(List<IServerService> view) {
            int numServers;
            if (view.Count == 0) {
                Console.WriteLine("No connections found. Broadcasting...");
                view = serverRemoteObjects;
                numServers = ConfigurationManager.AppSettings.AllKeys.Count(); //TODO use same file for client and server!!
            }
            else {
                numServers = view.Count;
            }
            WaitHandle[] handles = new WaitHandle[numServers];
            Console.WriteLine("Broadcasting to " + numServers + " servers...");
            IAsyncResult[] asyncResults = new IAsyncResult[numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                for (int i = 0; i < numServers; i++) {
                    IServerService remoteObject = view[i];
                    requestViewDelegate viewDel = new requestViewDelegate(remoteObject.ViewRequest);
                    Console.WriteLine(i);
                    IAsyncResult ar = viewDel.BeginInvoke(null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                }
                int indxAsync = WaitHandle.WaitAny(handles, 300); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) {
                    Console.WriteLine("timeout with " + numServers.ToString());
                    getView(view);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    requestViewDelegate viewDel = (requestViewDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    try {
                        List<string> servers = viewDel.EndInvoke(asyncResult);
                        if (servers.Count() != 0) {
                            List<IServerService> serverobjs = new List<IServerService>();
                            for (int j = 0; j < servers.Count; j++) {
                                Console.WriteLine("answer from " + indxAsync + ": " + servers[j] + "(" + servers.Count + ")");
                                serverobjs.Add((IServerService)Activator.GetObject(typeof(IServerService), servers[j]));
                            }
                            return serverobjs;
                        }
                        else {
                            return getView(view); //TODO? I dont like this
                        }
                    }
                    catch (SocketException) {
                        Console.WriteLine("ERROR: view is " + view.Count());
                        Console.WriteLine("Server " + indxAsync + " is down. Restarting...");
                        List<IServerService> newView = view;
                        
                        newView.RemoveAt(indxAsync);
                        Console.WriteLine("Trying again with " + newView.Count);
                        return getView(newView);
                    }
                    
                }
            } catch (SocketException e) {
                Console.WriteLine("Connection error. Restarting...");
                return getView(serverRemoteObjects); //TODO? isto acho que nao precisa de estar aqui (este catch)
            }
            Console.WriteLine("you shouldnt be here");
            return null;
        }
    }

 * */
