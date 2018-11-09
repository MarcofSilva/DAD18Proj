﻿using RemoteServicesLibrary;
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

namespace Client {
    public abstract class TupleSpaceAPI {

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

        public abstract void Write(ArrayList tuple);

        public abstract ArrayList Read(ArrayList tuple);

        public abstract ArrayList Take(ArrayList tuple);

        protected List<IServerService> prepareForRemoting(ref TcpChannel channel, int port) {
            //todo string myRemoteObjectName = "ClientService"; //TODO should the definition of this name be here?

            channel = new TcpChannel(port); //Port can't be 10000 (PCS) neither 10001 (Puppet Master)
            ChannelServices.RegisterChannel(channel, false);
            List<IServerService> serverRemoteObjects = new List<IServerService>();
            foreach (string url in ConfigurationManager.AppSettings.AllKeys) {
                serverRemoteObjects.Add((IServerService)Activator.GetObject(typeof(IServerService), url));
            }

            //todo RemotingServices.Marshal(myRemoteObject, myRemoteObjectName, typeof(ClientService));

            return serverRemoteObjects;
        }
    }
}
