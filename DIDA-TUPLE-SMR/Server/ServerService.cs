using RemoteServicesLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Configuration;
using System.Threading;
using ExceptionLibrary;

namespace Server
{
    class ServerService : MarshalByRefObject, IServerService
    {
        private Server _server;
        private int _min_delay;
        private int _max_delay;
        private Random random = new Random();

        private Dictionary<string, long> _nonceStorage = new Dictionary<string, long>();
        private Dictionary<string, IClientService> _remoteStorage = new Dictionary<string, IClientService>();

        public ServerService(Server server, int min_delay, int max_delay) {
            _server = server;
            _min_delay = min_delay;
            _max_delay = max_delay;
        }

        private bool validRequest(string clientURL, long nonce) {
            
            if (!_nonceStorage.ContainsKey(clientURL)) {
                _nonceStorage.Add(clientURL, nonce);
                _remoteStorage.Add(clientURL, (IClientService)Activator.GetObject(typeof(IClientService), clientURL));
                return true;
            }
            else { 
                long o = _nonceStorage[clientURL];
                if (nonce > o) {
                    _nonceStorage[clientURL] = nonce;
                    return true;
                }
                return false;
            }
        }

        public List<TupleClass> read(TupleClass tuple, string clientUrl, long nonce) {
            //TODO
            _server.checkFrozen();
            try {
                List<TupleClass> responseTuple = new List<TupleClass>();
                if (validRequest(clientUrl, nonce)) {
                    int r = random.Next(_min_delay, _max_delay);
                    Thread.Sleep(r);
                    responseTuple = _server.read(tuple, clientUrl, nonce);
                    return responseTuple;
                }
                return new List<TupleClass>();
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public TupleClass take(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            try {
                TupleClass responseTuple = new TupleClass();
                if (validRequest(clientUrl, nonce)) {
                    int r = random.Next(_min_delay, _max_delay);
                    Thread.Sleep(r);
                    responseTuple = _server.take(tuple, clientUrl, nonce);
                    return responseTuple;
                }//Update nonce info
                return new TupleClass();
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public void write(TupleClass tuple, string clientUrl, long nonce) {
            _server.checkFrozen();
            try {
                if (validRequest(clientUrl, nonce)) {
                    int r = random.Next(_min_delay, _max_delay);
                    Thread.Sleep(r);
                    _server.write(tuple, clientUrl, nonce);
                }
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public  EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            return _server.appendEntryWrite(writeEntry, term, leaderID);
        }

        public  EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            return _server.appendEntryTake(takeEntry, term, leaderID);
        }

        public EntryResponse heartBeat(int term, string candidateID) {
            return _server.heartBeat(term, candidateID);
        }

        public bool vote(int term, string candidateID) {
            return _server.vote(term, candidateID);
        }
        public void Freeze() {
            _server.Freeze();
        }
        public void Unfreeze() {
            _server.Unfreeze();
        }
    }
}
