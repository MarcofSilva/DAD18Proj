using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using RemoteServicesLibrary;

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

        public override List<TupleClass> Read(TupleClass tuple, string clientUrl, long nonce) {
            //Console.WriteLine(Server.serverRemoteObjects.Count);
            Console.WriteLine(_term);
            //enviar para o lider
            throw new NotImplementedException();
        }

        public override List<TupleClass> Take(TupleClass tuple, string clientUrl, long nonce) {
            //enviar para o lider
            throw new NotImplementedException();
        }

        public override void Write(TupleClass tuple, string clientUrl, long nonce) {
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
