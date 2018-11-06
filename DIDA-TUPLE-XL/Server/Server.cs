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
    class Server{
        private List<ArrayList> tupleContainer;
        TcpChannel channel;
        ServerService myRemoteObject;

        public Server(){
            tupleContainer = new List<ArrayList>();
            channel = new TcpChannel(8086); //TODO port
            ChannelServices.RegisterChannel(channel, false);
            myRemoteObject = new ServerService(this);
            RemotingServices.Marshal(myRemoteObject, "ServService", typeof(ServerService)); //TODO remote object name
            Console.WriteLine("<enter> to stop...");
            Console.ReadLine();
        }

        //void? devolve algo??
        public void write( ArrayList tuple){
            tupleContainer.Add(tuple);
            Console.WriteLine("write request");
            //Console.WriteLine(tupleContainer.Count);
            return;
        }

        //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> take(ArrayList tuple) {
            List<ArrayList> res = read(tuple);
            if (res.Count == 0) {
                Console.WriteLine("impossible to remove, no tuple in tuple space");
                return res;
            }
            tupleContainer.Remove(res[0]);
            Console.WriteLine("take container " + tupleContainer.Count);
            return res; 
        }

        public List<ArrayList> read(ArrayList tuple){
            List<ArrayList> res = new List<ArrayList>();
            //Console.WriteLine("initial read " + tupleContainer.Count + " container");
            Regex capital = new Regex(@"[A-Z]");
            foreach (ArrayList el in tupleContainer){
                bool add = true;
                if (el.Count != tuple.Count){
                    continue;
                }
                //sao do mesmo tamanho, vamos percorrer elemento a elemento nos 2
                for(int i = 0; i < tuple.Count; i++ ){
                    if (tuple[i] == null && el[i].GetType() != typeof(System.String)) {
                        break;
                    }

                    if (tuple[i] != null && !( (tuple[i].GetType() == typeof(System.String)) || (el[i].GetType() == tuple[i].GetType())) ){
                        add = false;
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
                            add = true;
                            break;
                        }
                        else if (tuple[i].GetType() == typeof(DADTestA)) {
                            DADTestA tuplei = (DADTestA)tuple[i];
                            DADTestA eli = (DADTestA)el[i];
                            if (!tuplei.Equals(eli)) {
                                add = false;
                            }
                            break;
                        }
                        else if (tuple[i].GetType() == typeof(DADTestB)) {
                            DADTestB tuplei = (DADTestB)tuple[i];
                            DADTestB eli = (DADTestB)el[i];
                            if (!tuplei.Equals(eli)) {
                                add = false;
                                break;
                            }
                            break;
                        }
                        else if(tuple[i].GetType() == typeof(DADTestC)) {
                            DADTestC tuplei = (DADTestC)tuple[i];
                            DADTestC eli = (DADTestC)el[i];
                            if (!tuplei.Equals(eli)) {
                                add = false;
                                break;
                            }
                            break;
                        }
                    }
                    add = false;
                }
                if (add) {
                    res.Add(el);
                }
            }
            Console.WriteLine("read container " + tupleContainer.Count + " read res " + res.Count);
            return res;
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
            Server server = new Server();
        }
    }
}
