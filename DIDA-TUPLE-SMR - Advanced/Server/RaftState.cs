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
using System.Threading;


namespace Server {
    public abstract class RaftState {
        protected int _term;
        protected string _url;
        protected string _leaderUrl;
        protected Server _server;
        protected int _numServers;
        protected List<string>  _view;
        protected Dictionary<string, IServerService> _serverRemoteObjects;

        public RaftState(Server server, int term) {
            _server = server;
            _term = term;
            _url = _server._url;
            _view = _server.fd.getView();
            _numServers = _view.Count();
            _serverRemoteObjects = server.serverRemoteObjects;
        }

        public abstract void stopClock();

        public abstract void startClock(int term, string url);

        public abstract void ping();

        public abstract EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID);

        public abstract bool vote(int term, string candidateID);

        public abstract List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce);

        public abstract TupleClass take(TupleClass tuple, string clientUrl, long nonce);

        public abstract void write(TupleClass tuple, string clientUrl, long nonce);
    }
}
