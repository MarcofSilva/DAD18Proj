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

        public void write(ArrayList tuple){
            //Console.WriteLine("Operation: " + tupleToString(tuple)); TODO tupleToString
            tupleSpace.Add(tuple);
            Console.WriteLine("Write done!");
        }

        //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> takeRead(ArrayList tuple) {
            /*List<ArrayList> res = read(tuple);
            if (res.Count == 0) {
                Console.WriteLine("impossible to remove, no tuple in tuple space");
                return res;
            }
            tupleSpace.Remove(res[0]);
            return res;*/
            return null;
        }

        public void takeRemove() {
            //TODO
        }

        public ArrayList read(ArrayList tuple){
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
                    if (tuple[i] == null && el[i].GetType() != typeof(System.String)) {
                        break;
                    }

                    if (tuple[i] != null && !( (tuple[i].GetType() == typeof(System.String)) || (el[i].GetType() == tuple[i].GetType())) ){
                        isMatch = false;
                        break;
                    }
                    if (el[i].GetType() == typeof(System.String)) { // estamos a ver uma string
                        if (tuple[i] != null && capital.IsMatch(tuple[i].ToString())) { }//request e objeto mas estamos a ver string
                        //request e um objeto
                        else if (tuple[i] != null && matchStrs(el[i], tuple[i])) {
                            break;
                        }
                    }
                    if (el[i].GetType() != typeof(System.String)) {
                        if (tuple[i].GetType() == typeof(System.String) && el[i].GetType().Name == tuple[i].ToString()) {
                            isMatch = true;
                            break;
                        }
                        else if (tuple[i].GetType() == typeof(DADTestA)) {
                            DADTestA tuplei = (DADTestA)tuple[i];
                            DADTestA eli = (DADTestA)el[i];
                            if (!tuplei.Equals(eli)) {
                                isMatch = false;
                            }
                            break;
                        }
                        else if (tuple[i].GetType() == typeof(DADTestB)) {
                            DADTestB tuplei = (DADTestB)tuple[i];
                            DADTestB eli = (DADTestB)el[i];
                            if (!tuplei.Equals(eli)) {
                                isMatch = false;
                                break;
                            }
                            break;
                        }
                        else if(tuple[i].GetType() == typeof(DADTestC)) {
                            DADTestC tuplei = (DADTestC)tuple[i];
                            DADTestC eli = (DADTestC)el[i];
                            if (!tuplei.Equals(eli)) {
                                isMatch = false;
                                break;
                            }
                            break;
                        }
                    }
                    isMatch = false;
                }
                if (isMatch) {
                    Console.WriteLine("Read: " + el); //TODO tupleToString(el)
                    return el;
                }
            }
            Console.WriteLine("Read: No match found!");
            return null; //no match
        }

        private bool matchStrs(object local, object request){
            string requeststr = (string)request;
            string localstr = (string)local;
            if (requeststr == "*") {
                if (local.GetType() == typeof(System.String)) {
                    return true;
                }
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
