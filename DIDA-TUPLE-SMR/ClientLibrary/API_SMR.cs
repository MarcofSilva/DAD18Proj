﻿using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class API_SMR : TupleSpaceAPI
    {
        //TODO estupidamente mal aqui, só para testar o resto...nao consegui usar o configuration file
        private ArrayList serverURLs;

        private TcpChannel channel;
        List<IServerService> serverRemoteObjects;

        public API_SMR() {
            //TODO estupidamente mal aqui, só para testar o resto...nao consegui usar o configuration file
            serverURLs = new ArrayList();
            serverURLs.Add("tcp://localhost:8086/ServService");

            serverRemoteObjects = prepareForRemoting(ref channel, serverURLs);
        }

        public override void Write(ArrayList tuple)
        {
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Write(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Read(ArrayList tuple)
        {
            //TODO
            //prints para debbug
            //Console.WriteLine("read in API_SMR: ");
            foreach (var item in tuple){
                if (item != null)
                {
                    //Console.WriteLine(item.ToString());
                }
                else
                {
                    Console.WriteLine("null");
                }
            }
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Read(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void Take(ArrayList tuple)
        {
            //TODO
            //prints para debbug
            //Console.Write("take in API_SMR: ");
            foreach (var item in tuple)
            {
                //Console.WriteLine(item.ToString());
            }
            try {
                foreach (IServerService remoteObject in serverRemoteObjects) {
                    remoteObject.Take(tuple, "url");
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
    }
}
