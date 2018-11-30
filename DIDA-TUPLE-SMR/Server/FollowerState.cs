using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Sockets;
using ClassLibrary;
using System.Runtime.Remoting.Messaging;
using System.Timers;

namespace Server {
    public class FollowerState : RaftState {
        private IServerService _leaderRemote;

        public FollowerState(Server server, int numServers) : base(server, numServers) {
            Console.WriteLine("Follower being Created");
        }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override void requestVote(int term, string candidateID) {
            throw new NotImplementedException();
        }

        
        public delegate List<TupleClass> readDelegate(TupleClass tuple, string url, long nonce);
        

        public override List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) { 
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                readDelegate readDel = new readDelegate(_leaderRemote.Read);
                asyncResults[0] = readDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                int indxAsync = WaitHandle.WaitAny(handles, 3000); //Wait for the first answer from the servers
                if (indxAsync == WaitHandle.WaitTimeout) { //if we have a timeout, due to no answer received with repeat the multicast TODO sera que querem isto
                    return read(tuple);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[indxAsync];
                    readDelegate readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    List<TupleClass> resTuple = readDel.EndInvoke(asyncResult);
                    nonce += 1;
                    if (resTuple.Count == 0) {
                        //Console.WriteLine("--->DEBUG: No tuple returned from server");
                        return new TupleClass();
                    }
                    return resTuple[0];
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate List<TupleClass> takeDelegate(TupleClass tuple, string url, long nonce);

        public override List<TupleClass> take(TupleClass tuple) {
            //enviar para o lider
            throw new NotImplementedException();
        }

        public delegate void writeDelegate(TupleClass tuple);

        public override void write(TupleClass tuple) {
            //enviar para o lider
            throw new NotImplementedException();
        }

        public override void electLeader(int term, string leaderUrl) {
            _term = term;
            _leaderUrl = leaderUrl;
            _leaderRemote = _serverRemoteObjects[_leaderUrl];
            Console.WriteLine("Follower: " + _server._url);
            Console.WriteLine("My leader is: " + leaderUrl);
        }
    }
}
