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
            //TODO resolver estas cenas de criar lista com apenas 1 elemento
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                readDelegate readDel = new readDelegate(_leaderRemote.read);
                asyncResults[0] = readDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple, clientUrl, nonce);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    readDel = (readDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    return readDel.EndInvoke(asyncResult);
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate List<TupleClass> takeDelegate(TupleClass tuple, string url, long nonce);

        public override List<TupleClass> take(TupleClass tuple, string clientUrl, long nonce) {
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                takeDelegate takeDel = new takeDelegate(_leaderRemote.take);
                asyncResults[0] = takeDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    return read(tuple, clientUrl, nonce);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    takeDel = (takeDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                    return takeDel.EndInvoke(asyncResult);
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public delegate void writeDelegate(TupleClass tuple, string clientUrl, long nonce);

        public override void write(TupleClass tuple, string clientUrl, long nonce) {
            WaitHandle[] handles = new WaitHandle[1];
            IAsyncResult[] asyncResults = new IAsyncResult[1];
            try {
                writeDelegate takeDel = new writeDelegate(_leaderRemote.write);
                asyncResults[0] = takeDel.BeginInvoke(tuple, clientUrl, nonce, null, null);
                handles[0] = asyncResults[0].AsyncWaitHandle;
                if (!WaitHandle.WaitAll(handles, 3000)) {
                    read(tuple, clientUrl, nonce);
                }
                else {
                    IAsyncResult asyncResult = asyncResults[0];
                    takeDel = (writeDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        public override void electLeader(int term, string leaderUrl) {
            _term = term;
            _leaderUrl = leaderUrl;
            _leaderRemote = _serverRemoteObjects[_leaderUrl];
            Console.WriteLine("Follower: " + _server._url);
            Console.WriteLine("My leader is: " + leaderUrl);
        }

        public override void ping() {
            Console.WriteLine("Follower State pinged");
        }
    }
}
