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
using ExceptionLibrary;

namespace Server {
    public class LeaderState : RaftState{
        private Random rnd = new Random(Guid.NewGuid().GetHashCode());
        private System.Timers.Timer timer;
        private int wait;

        private List<Entry> requestStorage = new List<Entry>();
        private List<Entry> answerStorage = new List<Entry>();
        
        
        public LeaderState(Server server, int numServers) : base(server, numServers) {
            _leaderUrl = server._url;
            SetTimer();
        }

        public override EntryResponse appendEntryWrite(WriteEntry writeEntry, int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Leader: AppendEntryWrite from: " + leaderID);
                _server.addEntrytoLog(writeEntry);
                _server.writeLeader(writeEntry.Tuple);
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getLogIndex()-1);
            }
            return new EntryResponse(false, _term, _server.getLogIndex());
        }

        public override EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Follower: AppendEntryTake from: " + leaderID);
                _server.addEntrytoLog(takeEntry);
                _server.writeLeader(takeEntry.Tuple);
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getLogIndex()-1);
            }
            return new EntryResponse(false, _term, _server.getLogIndex());
        }

        public override EntryResponse heartBeat(int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Leader: HeartBeat from: " + leaderID);
                //add this operation to log and then change to follower
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getLogIndex());
            }
            return new EntryResponse(false, _term, _server.getLogIndex());
        }

        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            try {
                return _server.readLeader(tuple, true);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public override TupleClass take(TupleClass tuple, string url, long nonce) {
            TupleClass realTuple = _server.readLeader(tuple, false)[0];
            TakeEntry entry = new TakeEntry(tuple, _term, _server.getLogIndex());
            
            //arranjar lock para o log e tuplespace(?)
            _server.addEntrytoLog(entry);

            pulseAppendEntryTake(entry);

            return _server.takeLeader(tuple);
        }

        //TODO falta utilizar nounce
        public override void write(TupleClass tuple, string url, long nonce) {
            try {
                WriteEntry entry = new WriteEntry(tuple, _term, _server.getLogIndex());
                //arranjar lock para o log
                _server.addEntrytoLog(entry);

                pulseAppendEntryWrite(entry);
                _server.writeLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        //TODO falta utilizar nounce
        public delegate EntryResponse appendEntryTakeDelegate(TakeEntry takeEntry, int term, string leaderID);
        public delegate EntryResponse appendEntryWriteDelegate(WriteEntry takeEntry, int term, string leaderID);
        public delegate EntryResponse heartBeatDelegate(int term, string candidateID);

        public void pulseAppendEntryTake(TakeEntry takeEntry) {
            int sucess = 0;
            timer.Interval = wait;
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers];
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    appendEntryTakeDelegate appendEntryTakeDel = new appendEntryTakeDelegate(remoteObject.appendEntryTake);
                    IAsyncResult ar = appendEntryTakeDel.BeginInvoke(takeEntry, _term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {

                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        appendEntryTakeDelegate appendEntryTakeDel = (appendEntryTakeDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = appendEntryTakeDel.EndInvoke(asyncResult);
                        if (!response.Sucess) {//foi false
                            if (_term < response.Term) {//term da resposta e maior do que o meu
                                _server.updateState("follower", _term, "");
                                //TODO
                                //isto esta mal, temos de ter o url do lider para quando for para follower saber quem e
                            }
                            else {//o meu log esta mais avancado que o dele

                            }
                        }
                        if (response.Sucess) {
                            sucess++;
                        }
                    }
                    if (!(sucess > _numServers / 2)) {
                        pulseAppendEntryTake(takeEntry);
                    }
                }
            }
            catch (ElectionException) {
                //nao sei se e preciso tratar visto que nao pode existir 1 lider e 1 candidato ao mesmo tempo
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        public void pulseAppendEntryWrite(WriteEntry writeEntry) {
            int sucess = 0;
            timer.Interval = wait;
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers];
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    appendEntryWriteDelegate appendEntryWriteDel = new appendEntryWriteDelegate(remoteObject.appendEntryWrite);
                    IAsyncResult ar = appendEntryWriteDel.BeginInvoke(writeEntry, _term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {

                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        appendEntryWriteDelegate appendEntryWriteDel = (appendEntryWriteDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = appendEntryWriteDel.EndInvoke(asyncResult);
                        if (!response.Sucess) {//foi false
                            if (_term < response.Term) {//term da resposta e maior do que o meu
                                //TODO
                                //isto esta mal, temos de ter o url do lider para quando for para follower saber quem e
                                _server.updateState("follower", _term, "");

                            }
                            else {//o meu log esta mais avancado que o dele

                            }
                        }
                        if (response.Sucess) {
                            sucess++;
                        }
                    }
                    if (!(sucess > _numServers / 2)) {
                        pulseAppendEntryWrite(writeEntry);
                    }
                }
            }
            catch (ElectionException) {
                //nao sei se e preciso tratar visto que nao pode existir 1 lider e 1 candidato ao mesmo tempo
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }
        private void pulseHeartbeat() {
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers]; //used when want to access IAsyncResult in index of handled that give the signal
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    heartBeatDelegate heartBeatDel = new heartBeatDelegate(remoteObject.heartBeat);
                    IAsyncResult ar = heartBeatDel.BeginInvoke(_term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 1000)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {
                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = heartBeatDel.EndInvoke(asyncResult);
                        if (!response.Sucess) {//foi false
                            if (_term < response.Term) {//term da resposta e maior do que o meu
                                //TODO
                                //isto esta mal, temos de ter o url do lider para quando for para follower saber quem e
                                _server.updateState("follower", _term, "");

                            }
                            else {//o meu log esta mais avancado que o dele
                                //tratar deste caso
                            }  
                        }
                        //Console.WriteLine("heartbeat to " + i + " was " + response.Sucess);
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
        }

        private void SetTimer() {
            wait = rnd.Next(150, 300);
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Stop();
        }

        public override void stopClock() {
            timer.Stop();
        }

        public override void startClock(int term, string url) {
            if (term > _term) {
                _term = term;
            }
            pulseHeartbeat();
            timer.Start();
            //redundante porque o url recebido e o dele proprio
            _leaderUrl = url;
        }
      
        public override void ping() {
            Console.WriteLine("Leader State pinged ");
        }

        public override bool vote(int term, string candidateID) {
            Console.WriteLine("I WAS IN TERM " + _term + " AND THEY ARE IN TERM " + term);
            if (_term < term) {
                _term = term;
                _server.updateState("follower", _term, candidateID);
                return true;
            }
            //the system doesnt alow it to come here
            return false;
        }
    }
}
    