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
        private bool timerThreadBlock = false;

        private List<Entry> requestStorage = new List<Entry>();
        private List<Entry> answerStorage = new List<Entry>();
        private readonly Object vote_heartbeat_Lock = new object();

        public LeaderState(Server server) : base(server) {
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

                    //TODO PEDIR AO SOUSA PARA EXPLICAR, FOLHA DO MARCO
                    if ((_server.getLogIndex() - 1 + entryPacket.Count) != entryPacket.Entrys[entryPacket.Count - 1].LogIndex)
                    {
                        //envio o server log index e isso diz quantas entrys tem o log, do lado de la, ele ve 
                        Console.WriteLine("Leader -> Follower : appendEntry");

                        _server.updateState("follower", _term, leaderID);
                        timerThreadBlock = true;
                        return new EntryResponse(false, _term, _server.getLogIndex());
                    }
                    foreach (Entry entry in entryPacket.Entrys)
                    {
                        _server.addEntrytoLog(entry);
                        //TODO, matilde queres meter a comparacao de strings como gostas? xD
                        if (entry.Type == "write")
                        {
                            _server.writeLeader(entry.Tuple);
                        }
                        else
                        {
                            _server.takeLeader(entry.Tuple);
                        }
                    }
                    Console.WriteLine("Leader -> Follower : appendEntry");
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, leaderID);
                    //envio o server log index porque quando o servidor me enviar isto ele ja vai ter adicionado ao log dele
                    //logo na resposta vou comparar _server.logIndex do lado de lado com o deste
                    //na verdade o que estao a ver e: se deu true, entao eu tenho tantas packets como quem me respondeu
                    //se bem que visto que esta true ele n vai verificar nada do log index ou term
                    return new EntryResponse(true, _term, _server.getLogIndex());
                }
                return new EntryResponse(false, _term, _server.getLogIndex());
            }
        }

        public override List<TupleClass> read(TupleClass tuple, string url, long nonce) {
            try {
                return _server.readLeader(tuple, true);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        public void pulseHeartbeat() {
            EntryPacket entryPacket = new EntryPacket();
            pulseAppendEntry(entryPacket);
        }

        public override TupleClass take(TupleClass tuple, string url, long nonce) {
            TupleClass realTuple = _server.readLeader(tuple, false)[0];
            TakeEntry entry = new TakeEntry(tuple, _term, _server.getLogIndex(), "take");
            //arranjar lock para o log e tuplespace(?)
            _server.addEntrytoLog(entry);

            List<Entry> packet = new List<Entry>();
            packet.Add(entry);
            EntryPacket entryPacket = new EntryPacket(packet);

            timer.Interval = wait;
            pulseAppendEntry(entryPacket);

            return _server.takeLeader(tuple);
        }

        //TODO falta utilizar nounce
        public override void write(TupleClass tuple, string url, long nonce) {
            try {
                WriteEntry entry = new WriteEntry(tuple, _term, _server.getLogIndex(), "write");
                //arranjar lock para o log
                _server.addEntrytoLog(entry);

                List<Entry> packet = new List<Entry>();
                packet.Add(entry);
                EntryPacket entryPacket = new EntryPacket(packet);

                timer.Interval = wait;
                pulseAppendEntry(entryPacket);
                _server.writeLeader(tuple);
            }
            catch (ElectionException e) {
                throw e;
            }
        }

        //TODO falta utilizar nounce
        public delegate EntryResponse appendEntryDelegate(EntryPacket entryPacket, int term, string leaderID);

        public void pulseAppendEntry(EntryPacket entryPacket) {
            
            if (timerThreadBlock) {
                return;
            }
            
            int sucess = 1;
            timer.Enabled = true;
            if (_server.fd.changed()) {
                _view = _server.fd.getView();
                _numServers = _view.Count();
            }
            WaitHandle[] handles = new WaitHandle[_numServers-1];
            IAsyncResult[] asyncResults = new IAsyncResult[_numServers-1];
            try {
                int i = 0;
                foreach (string url in _view) {
                    if (url == _url) {
                        continue;
                    }
                    ServerService remoteObject = (ServerService)_serverRemoteObjects[url];
                    appendEntryDelegate appendEntryDel = new appendEntryDelegate(remoteObject.appendEntry);
                    IAsyncResult ar = appendEntryDel.BeginInvoke(entryPacket, _term, _url, null, null);
                    asyncResults[i] = ar;
                    handles[i] = ar.AsyncWaitHandle;
                    i++;
                }
                if (!WaitHandle.WaitAll(handles, 5000)) {//TODO esta desoncronizado
                    pulseHeartbeat();
                }
                else {
                    for (i = 0; i < _numServers-1; i++) {
                        IAsyncResult asyncResult = asyncResults[i];
                        appendEntryDelegate appendEntryDel = (appendEntryDelegate)((AsyncResult)asyncResult).AsyncDelegate;
                        EntryResponse response = appendEntryDel.EndInvoke(asyncResult);
                        if (!response.Sucess) {//foi false
                            if (_term < response.Term) {//term da resposta e maior do que o meu
                                Console.WriteLine("Leader -> Follower : pulseappendEntry");
                                timerThreadBlock = true;
                                _server.updateState("follower", _term, response.Leader);
                            }
                            else {//o meu log esta mais avancado que o dele
                                //ter um set dos servidores que estao up to date
                                //no proximo append entry vemos se algum servidor esta para tras e dps e enviar tudo
                                //ele na resposta envia o log index dele  = quantas packets ele tem, logo temos de enviar
                                //as ultimas nosso log - log da resposta entradas
                            }
                        }
                        if (response.Sucess) {
                            sucess++;
                        }
                    }
                    if (!(sucess > (_numServers / 2))) {
                        pulseAppendEntry(entryPacket);
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


        private void OnTimedEvent(Object source, ElapsedEventArgs e) {
            pulseHeartbeat();
        }

        private void SetTimer() {
            wait = rnd.Next(200, 300);
            timer = new System.Timers.Timer(wait);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = false;
            timer.Enabled = false;
        }

        public override void stopClock() {
            timer.Stop();
        }

        public override void startClock(int term, string url) {
            if (term > _term) {
                _term = term;
            }
            timerThreadBlock = false;
            pulseHeartbeat();
            timer.Start();
            //TODO redundante porque o url recebido e o dele proprio
            _leaderUrl = url;
        }
      
        public override void ping() {
            Console.WriteLine("Leader State pinged ");
        }

        public override bool vote(int term, string candidateID)
        {
            lock (vote_heartbeat_Lock)
            {
                Console.WriteLine("I WAS IN TERM " + _term + " AND THEY ARE IN TERM " + term);
                if (_term < term)
                {
                    _term = term;
                    Console.WriteLine("Leader -> Follower : vote for " + candidateID);
                    timerThreadBlock = true;
                    _server.updateState("follower", _term, candidateID);
                    return true;
                }
                //the system doesnt alow it to come here
                return false;
            }
        }
    }
}
    