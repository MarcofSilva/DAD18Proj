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
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
            return new EntryResponse(false, _term, 0);
        }

        public override EntryResponse appendEntryTake(TakeEntry takeEntry, int term, string leaderID) {
            if (_term < term) {
                _term = term;
                Console.WriteLine("Follower: AppendEntryTake from: " + leaderID);
                _server.addEntrytoLog(takeEntry);
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
            return new EntryResponse(false, _term, 0);
        }

        public override EntryResponse heartBeat(int term, string leaderID) {
            if (_term < term) {
                _term = term;
                //Console.WriteLine("Follower: HeartBeat from: " + leaderID);
                //add this operation to log and then change to follower
                _server.updateState("follower", _term, leaderID);
                return new EntryResponse(true, _term, _server.getMatchIndex());
            }
            return new EntryResponse(false, _term, 0);
        }

        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            try {
                return _server.readLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public override TupleClass take(TupleClass tuple, string url, long nonce) {
            TakeEntry entry = new TakeEntry(tuple, _term);
            _server.addEntrytoLog(entry);
            requestStorage.Add(entry);
            while (answerStorage.Count == 0) {

            }
            return answerStorage[0].Tuple;
        }

        public delegate EntryResponse appendEntryTakeDelegate(TakeEntry takeEntry, int term, string leaderID);

        public void pulseAppendEntryTake(TakeEntry takeEntry) {
            int sucess = 0;
            WaitHandle[] handles = new WaitHandle[_numServers];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers];
            try {
                int i = 0;
                foreach (KeyValuePair<string, IServerService> remoteObjectpair in _serverRemoteObjects) {
                    ServerService remoteObject = (ServerService)remoteObjectpair.Value;
                    appendEntryTakeDelegate appendEntryTakeDel = new appendEntryTakeDelegate(remoteObject.appendEntryTake);
                    IAsyncResult ar = appendEntryTakeDel.BeginInvoke(takeEntry, _term ,_url, null, null);
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
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = heartBeatDel.EndInvoke(asyncResult);
                        if (response.Sucess) {
                            sucess++;
                        }
                        //falta dar update para quais e que teve sucesso
                        //update da lista dos servidores etc

                        //IDEIA em vez de controlar quais tiveram sucessos, mandar para todos ate todos responderem com controlo de nounce
                    }
                    if (sucess > _numServers / 2) {
                        //commit entry?
                        //meter a entry no answerStorage
                        //desbloquear o metodo
                        //enviar acks para todos
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

        public override void write(TupleClass tuple, string url, long nonce) {
            try {
                WriteEntry entry = new WriteEntry(tuple, _term);
                _server.writeLeader(tuple);
                requestStorage.Add(entry);

            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public delegate EntryResponse appendEntryWriteDelegate(WriteEntry takeEntry, int term, string leaderID);

        public void pulseAppendEntryWrite(WriteEntry writeEntry) {
            int sucess = 0;
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
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = heartBeatDel.EndInvoke(asyncResult);
                        if (response.Sucess) {
                            sucess++;
                        }
                        //falta dar update para quais e que teve sucesso
                        //update da lista dos servidores etc

                        //IDEIA em vez de controlar quais tiveram sucessos, mandar para todos ate todos responderem com controlo de nounce
                    }
                    if (sucess > _numServers / 2) {
                        //commit entry?
                        //meter a entry no answerStorage
                        //desbloquear o metodo
                        //enviar acks para todos
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

        public delegate EntryResponse heartBeatDelegate(int term, string candidateID);

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
                if (!WaitHandle.WaitAll(handles, 5000)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {
                    for (i = 0; i < _numServers; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        heartBeatDelegate heartBeatDel = (heartBeatDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = heartBeatDel.EndInvoke(asyncResult);
                        Console.WriteLine("heartbeat to " + i +" was " +response.Sucess);
                    }
                }
            }
            catch (SocketException) {
                //TODO
                throw new NotImplementedException();
            }
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            if(requestStorage.Count > 0) {
                if (requestStorage[0].GetType() == typeof(WriteEntry)) {
                    //TODO verificar casts
                    pulseAppendEntryWrite((WriteEntry)requestStorage[0]);
                }
                else {
                    //TODO verificar casts
                    pulseAppendEntryTake((TakeEntry)requestStorage[0]);
                }
            }
            else {
                pulseHeartbeat();
            }
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
    