using RemoteServicesLibrary;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server{
    public class Server{
        private List<ArrayList> tupleSpace;
        private TcpChannel channel;
        private ServerService myRemoteObject;
        private const int defaultPort = 8086;
        private const string defaultname = "Server";

        public Server(){
            prepareRemoting(defaultPort, defaultname);
        }

        public Server(string URL) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int port;
            Int32.TryParse(urlSplit[2], out port);

            prepareRemoting(port, urlSplit[3]);
            Console.WriteLine("Hello! I'm a Server at port " + urlSplit[2]);
        }

        private void prepareRemoting(int port, string name) {
            tupleSpace = new List<ArrayList>();
            channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ServerService(this);
            RemotingServices.Marshal(myRemoteObject, name, typeof(ServerService)); //TODO remote object name
        }

        private bool compareTuple(ArrayList tuplei, ArrayList tupleo) {
            //TODOOO
            if(printTuple(tuplei) != printTuple(tupleo)) {
                return false;
            }
            return true;
        }

        private String printTuple(ArrayList tuple) {
            string acc = "<";
            for (int i = 0; i < tuple.Count; i++) {
                if (i != 0) {
                    acc += ",";
                }
                if (tuple[i].GetType() == typeof(System.String)) {
                    acc += "\"" + tuple[i].ToString() + "\"";
                }
                else if(tuple[i] == typeof(DADTestA)) {
                    acc += "DADTestA";
                }
                else if (tuple[i] == typeof(DADTestB)) {
                    acc += "DADTestB";
                }
                else if (tuple[i] == typeof(DADTestC)) {
                    acc += "DADTestC";
                }
                else {
                    acc += tuple[i].ToString();
                }
            }
            acc += ">";
            return acc;
        }

        public void write(ArrayList tuple){
            //Console.WriteLine("Operation: " + tupleToString(tuple)); TODO tupleToString
            tupleSpace.Add(tuple);
            Console.WriteLine("Writed: " + printTuple(tuple) + "\n");
        }

        public void takeRemove(ArrayList tuple) {
            //TODO martelo
            foreach (ArrayList el in tupleSpace) {
                if(compareTuple(tuple, el)) {
                    tupleSpace.Remove(el);
                    return;
                }
            }
        }

        //e basicamente igual ao read mas com locks nas estruturas
        public List<ArrayList> takeRead(ArrayList tuple) {
            List<ArrayList> res = new List<ArrayList>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (ArrayList el in tupleSpace) {
                bool isMatch = true;
                if (el.Count != tuple.Count) {
                    continue;

                }
                //sao do mesmo tamanho, vamos percorrer elemento a elemento nos 2
                for (int i = 0; i < tuple.Count; i++) {
                    //pedido e um null e estamos a ver um objeto
                    if (tuple[i] == null && el[i].GetType() != typeof(System.String)) {
                        continue;
                    }
                    //se o pedido nao e null, para passar ou sao os 2 strings ou 2 nao sao string
                    if (tuple[i] != null && !((tuple[i].GetType() == typeof(System.String)) && (el[i].GetType() == typeof(System.String)) ||
                                              (tuple[i].GetType() != typeof(System.String)) && (el[i].GetType() != typeof(System.String)))) {
                        //Console.WriteLine("um e string e o outro nao");
                        isMatch = false;
                        break;
                    }
                    //se estamos aqui ou sao os 2 strings ou os 2 objetos
                    if (el[i].GetType() == typeof(System.String)) {
                        if (!matchStrs(el[i], tuple[i])) {
                            //Console.WriteLine("--------->strings dont match ");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    if (tuple[i] == typeof(DADTestA) && el[i].GetType() == typeof(DADTestA)) {
                        //Console.WriteLine("asked for type DADTestA and there is one");
                        continue;
                    }
                    else if (tuple[i] == typeof(DADTestB) && el[i].GetType() == typeof(DADTestB)) {
                        //Console.WriteLine("asked for type DADTestB and there is one");
                        continue;
                    }
                    else if (tuple[i] == typeof(DADTestC) && el[i].GetType() == typeof(DADTestC)) {
                        //Console.WriteLine("asked for type DADTestC and there is one");
                        continue;
                    }
                    else if (tuple[i].GetType() == typeof(DADTestA) && el[i].GetType() == typeof(DADTestA)) {
                        //Console.WriteLine("------------------> DADTestA");
                        DADTestA tuplei = (DADTestA)tuple[i];
                        DADTestA eli = (DADTestA)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTA");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    else if (tuple[i].GetType() == typeof(DADTestB) && el[i].GetType() == typeof(DADTestB)) {
                        //Console.WriteLine("------------------> DADTestB");
                        DADTestB tuplei = (DADTestB)tuple[i];
                        DADTestB eli = (DADTestB)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTB");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    else if (tuple[i].GetType() == typeof(DADTestC) && el[i].GetType() == typeof(DADTestC)) {
                        //Console.WriteLine("------------------> DADTestC");
                        DADTestC tuplei = (DADTestC)tuple[i];
                        DADTestC eli = (DADTestC)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTC");
                            isMatch = false;
                            break;
                        }
                        break;
                    }
                    //Console.WriteLine("reached end");
                    isMatch = false;
                }
                if (isMatch) {
                    Console.WriteLine("TakeRead: Added: " + printTuple(el) + "to res \n");
                    res.Add(el);//esta a devolver o primeiro que encontrou, n esta a devolver todos os que dao match
                }
            }
            return res; //no match
        }

        public List<ArrayList> read(ArrayList tuple){
            
            List<ArrayList> res = new List<ArrayList>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (ArrayList el in tupleSpace){
                bool isMatch = true;
                if (el.Count != tuple.Count){
                    continue;
                    
                }
                //sao do mesmo tamanho, vamos percorrer elemento a elemento nos 2
                for(int i = 0; i < tuple.Count; i++ ){
                    //pedido e um null e estamos a ver um objeto
                    if (tuple[i] == null && el[i].GetType() != typeof(System.String)) {
                        continue ;
                    }
                    //se o pedido nao e null, para passar ou sao os 2 strings ou 2 nao sao string
                    if (tuple[i] != null && !((tuple[i].GetType() == typeof(System.String)) && (el[i].GetType() == typeof(System.String)) ||
                                              (tuple[i].GetType() != typeof(System.String)) && (el[i].GetType() != typeof(System.String)) )){
                        //Console.WriteLine("um e string e o outro nao");
                        isMatch = false;
                        break;
                    }
                    //se estamos aqui ou sao os 2 strings ou os 2 objetos
                    if (el[i].GetType() == typeof(System.String)) {
                        if (!matchStrs(el[i], tuple[i])) {
                            //Console.WriteLine("--------->strings dont match ");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    if (tuple[i] == typeof(DADTestA) && el[i].GetType() == typeof(DADTestA)) {
                        //Console.WriteLine("asked for type DADTestA and there is one");
                        continue;
                    }
                    else if (tuple[i] == typeof(DADTestB) && el[i].GetType() == typeof(DADTestB)) {
                        //Console.WriteLine("asked for type DADTestB and there is one");
                        continue;
                    }
                    else if (tuple[i] == typeof(DADTestC) && el[i].GetType() == typeof(DADTestC)) {
                        //Console.WriteLine("asked for type DADTestC and there is one");
                        continue;
                    }
                    else if (tuple[i].GetType() == typeof(DADTestA) && el[i].GetType() == typeof(DADTestA)) {
                        //Console.WriteLine("------------------> DADTestA");
                        DADTestA tuplei = (DADTestA)tuple[i];
                        DADTestA eli = (DADTestA)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTA");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    else if (tuple[i].GetType() == typeof(DADTestB) && el[i].GetType() == typeof(DADTestB)) {
                        //Console.WriteLine("------------------> DADTestB");
                        DADTestB tuplei = (DADTestB)tuple[i];
                        DADTestB eli = (DADTestB)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTB");
                            isMatch = false;
                            break;
                        }
                        continue;
                    }
                    else if(tuple[i].GetType() == typeof(DADTestC) && el[i].GetType() == typeof(DADTestC)) {
                        //Console.WriteLine("------------------> DADTestC");
                        DADTestC tuplei = (DADTestC)tuple[i];
                        DADTestC eli = (DADTestC)el[i];
                        if (!tuplei.Equals(eli)) {
                            //Console.WriteLine("objetos nao sao iguais DADTESTC");
                            isMatch = false;
                            break;
                        }
                        break;
                    }
                    //Console.WriteLine("reached end");
                    isMatch = false;
                }
                if (isMatch) {
                    //Console.WriteLine("Read: Added: " + printTuple(el) + "to return tuples \n");
                    res.Add(el);//esta a devolver o primeiro que encontrou, n esta a devolver todos os que dao match
                }
            }
            return res; //no match
        }

        private bool matchStrs(object local, object request){
            string requeststr = (string)request;
            string localstr = (string)local;
            if (requeststr == "*") {
                return true;
            }
            if (requeststr.Contains("*")) {
                string regex = "";
                if(requeststr[0].ToString() == "*") {
                    //quero o resto da string menos o primeiro elemento
                    regex = ".*" + requeststr.Substring(1) + "$";
                }
                else {
                    //quero o resto da string menos o *
                    regex = "^" + requeststr.Substring(0, (requeststr.Length -1)) + ".*";
                }
                Regex wildcard = new Regex(regex);
                if (wildcard.IsMatch(localstr)){
                    return true;
                }
            }
            //a partir daqui wild cards tratadas, so falta tratar se as strings sao mesmo iguais
            if (requeststr == localstr) {
                return true;
            }
            return false;
        }

        static void Main(string[] args){
            Server server;
            if(args.Length == 0) {
                server = new Server();
            }
            else {
                server = new Server(args[0]);
            }

            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }
    }
}
