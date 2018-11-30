using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    public class CandidateState : RaftState {

        public CandidateState(Server server, int numServers) : base(server, numServers) { }

        public override void apprendEntry(int term, string senderID) {
            throw new NotImplementedException();
        }

        public override void requestVote(int term, string candidateID) {
            throw new NotImplementedException();
        }

        public override List<TupleClass> Read(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }

        public override List<TupleClass> Take(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }

        public override void Write(TupleClass tuple, string clientUrl, long nonce) {
            throw new NotImplementedException();
        }

        public override void electLeader(int term, string leaderUrl) {
            throw new NotImplementedException();
        }
    }
}
