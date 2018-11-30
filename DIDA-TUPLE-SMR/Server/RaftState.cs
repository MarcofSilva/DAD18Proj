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


namespace Server {
    public abstract class RaftState {
        protected int _term;
        protected string _leaderUrl;
        protected Server _server;
        protected int _numServers;
        protected Dictionary<string, IServerService> _serverRemoteObjects;

        public RaftState(Server server, int numServers) {
            _server = server;
            _numServers = numServers;
            _term = 1;
            _serverRemoteObjects = server.serverRemoteObjects;
        }

        public abstract void apprendEntry(int term, string senderID);

        public abstract void requestVote(int term, string candidateID);

        public abstract List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce);

        public abstract List<TupleClass> take(TupleClass tuple, string clientUrl, long nonce);

        public abstract void write(TupleClass tuple, string clientUrl, long nonce);

        //temporary function for testing
        public abstract void electLeader(int term, string leaderUrl);
    }
}
