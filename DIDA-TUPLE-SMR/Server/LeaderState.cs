﻿using RemoteServicesLibrary;
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
        private bool timerThreadBlock = false;
        private bool clockWasRunning = true;

        
        private readonly Object vote_heartbeat_Lock = new object();

        public LeaderState(Server server, int term) : base(server, term) {
            _leaderUrl = server._url;
            SetTimer();
        }

        public override EntryResponse appendEntry(EntryPacket entryPacket, int term, string leaderID)
        {
            lock (vote_heartbeat_Lock)
            {
                if (_term < term)
                {
                    _term = term;
                    Console.WriteLine("Leader: AppendEntry from: " + leaderID);

                    if ((_server.getLogIndex() - 1 + entryPacket.Count) != entryPacket.Entrys[entryPacket.Count - 1].LogIndex)
                    {
                        //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                        Console.WriteLine("Leader -> Follower : appendEntry");

                        _server.updateState("follower", _term, leaderID);
                        _server = null;
                        timer.Dispose();
                        timerThreadBlock = true;
                        return new EntryResponse(false, _term, _server.getLogIndex());
                    }
                    foreach (Entry entry in entryPacket.Entrys)
                    {
                        if (entry.Type == "write")
                        {
                            _server.addEntrytoLog(entry);
                            _server.writeLeader(entry.Tuple);
                        }
                        else
                        {
                            _server.takeLeader(entry.Tuple, entry.Term);
                        }
                    }
                    Console.WriteLine("Leader -> Follower : appendEntry");
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, leaderID);
                    _server = null;
                    timer.Dispose();
                    //envio o server log index porque quando o servidor me enviar isto ele ja vai ter adicionado ao log dele
                    //logo na resposta vou comparar _server.logIndex do lado de lado com o deste
                    //na verdade o que estao a ver e: se deu true, entao eu tenho tantas packets como quem me respondeu
                    //se bem que visto que esta true ele n vai verificar nada do log index ou term
                    return new EntryResponse(true, _term, _server.getLogIndex());
                }
                return new EntryResponse(false, _term, _server.getLogIndex());
            }
        }

        public override TupleClass read(TupleClass tuple, string url, long nonce) {
            try {
                return _server.readLeader(tuple, true);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public void pulseHeartbeat() {
            pulseAppendEntry();
        }

        public override TupleClass take(TupleClass tuple, string url, long nonce)
        {
            TupleClass realTuple = _server.readLeader(tuple, false);
            timer.Interval = wait;
            TupleClass res = _server.takeLeader(tuple, _term);
            pulseAppendEntry();
            return res;
        }

        public override void write(TupleClass tuple, string url, long nonce) {
            try {
                WriteEntry entry = new WriteEntry(tuple, _term, _server.getLogIndex(), "write");
                //arranjar lock para o log
                _server.addEntrytoLog(entry);

                timer.Interval = wait;
                pulseAppendEntry();
                _server.writeLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public delegate EntryResponse appendEntryDelegate(EntryPacket entryPacket, int term, string leaderID);

        public void pulseAppendEntry() {
            if (timerThreadBlock) {
                return;
            }
            int sucess = 1;
            timer.Enabled = true;
            if (_server.fd.changed()) {
                _view = _server.fd.getView();
                _numServers = _view.Count();
            }
            Dictionary<int, string> i_url_map = new Dictionary<int, string>();
            WaitHandle[] handles = new WaitHandle[_numServers-1];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers-1];
            try {
                int i = 0;
                foreach (string url in _view) {
                    if (url == _url) {
                        continue;
                    }
                    EntryPacket entryPacket = new EntryPacket();
                    int theirIndex = _server.matchIndexMap[url];
                    int myindex = _server.getLogIndex();
                    if (myindex != theirIndex ) {
                        for (int k = theirIndex; k < myindex; k++) {
                            entryPacket.Add(_server.entryLog[k]);
                        }
                    }
                    ServerService remoteObject = (ServerService)_serverRemoteObjects[url];
                    appendEntryDelegate appendEntryDel = new appendEntryDelegate(remoteObject.appendEntry);
                    IAsyncResult ar = appendEntryDel.BeginInvoke(entryPacket, _term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i_url_map.Add(i, url);
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 150)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {
                    foreach (KeyValuePair<int, string> entry in i_url_map) {
                        IAsyncResult asyncResult = asyncResults[entry.Key];
                        appendEntryDelegate appendEntryDel = (appendEntryDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = appendEntryDel.EndInvoke(asyncResult);
                        if (!response.Sucess) {//foi false
                            if (_term < response.Term) {//term da resposta e maior do que o meu
                                Console.WriteLine("Leader -> Follower : pulseappendEntry");
                                timerThreadBlock = true;
                                _server.updateState("follower", _term, response.Leader);
                                _server = null;
                                timer.Dispose();
                            }
                            //nao estou a tratar quando da false por causa do log aqui especificamente
                            //trato tudo da mesma maneira
                        }
                        //se tiver ter dado true e porque os 2 estao up to date, logo atualizo o dele para o bem
                        //se tiver dado false, meto no mapa nao contando para os sucessos,
                        //depois no heartbeat a seguir deve sincronizar teoricamente :)
                        _server.matchIndexMap[entry.Value] = response.MatchIndex;
                        if (response.Sucess) {
                            sucess++;
                        }
                    }
                    if (!(sucess > (_numServers / 2))) {
                        pulseAppendEntry();
                    }

                }
            }
            catch (ElectionException) {
                pulseAppendEntry();
            }
            catch (SocketException) {
                pulseAppendEntry();
            }
        }


        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
        }

        private void SetTimer() {
            wait = 100;
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = false;
            timer.Enabled = true;
        }

        public override void stopClock() {
            timer.Stop();
            timer.Dispose();
        }

        public override void startClock(int term, string url) {
            if (term > _term) {
                _term = term;
            }
            timerThreadBlock = false;
            pulseHeartbeat();
            SetTimer();
            _leaderUrl = url;
        }

        public override void playClock() {
            if (clockWasRunning) {
                timer.Start();
            }
        }

        public override void pauseClock() {
            if (timer.Enabled) {
                clockWasRunning = true;
                timer.Stop();
            }
            else clockWasRunning = false;
        }

        public override void ping() {
        }

        public override bool vote(int term, string candidateID)
        {
            lock (vote_heartbeat_Lock)
            {
                if (_term < term)
                {
                    _term = term;
                    Console.WriteLine("Leader -> Follower : vote for " + candidateID);
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, candidateID);
                    _server = null;
                    timer.Dispose();
                    return true;
                }
                //the system doesnt alow it to come here
                return false;
            }
        }
    }
}
    