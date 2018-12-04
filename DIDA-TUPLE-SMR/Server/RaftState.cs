using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using RemoteServicesLibrary;
using ExceptionLibrary;


namespace Server {
    public abstract class RaftState {
        protected int _term;
        protected string _url;
        protected string _leaderUrl;
        protected Server _server;
        protected int _numServers;
        protected Dictionary<string, IServerService> _serverRemoteObjects;

        public RaftState(Server server, int numServers) {
            _server = server;
            _numServers = numServers;
            _term = 0;
            _url = _server._url;
            _serverRemoteObjects = server.serverRemoteObjects;
        }

        public abstract void stopClock();

        public abstract void startClock(int term, string url);

        public abstract void ping();

        public abstract EntryResponse heartBeat(int term, string leaderID);

        public abstract EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID);

        public abstract EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID);

        public abstract bool vote(int term, string candidateID);

        public abstract List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce);

        public abstract TupleClass take(TupleClass tuple, string clientUrl, long nonce);

        public abstract void write(TupleClass tuple, string clientUrl, long nonce);
    }
}
