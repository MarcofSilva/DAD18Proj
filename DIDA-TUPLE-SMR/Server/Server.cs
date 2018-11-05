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
            //Console.WriteLine(tuple[0].GetType().ToString());
            tupleContainer.Add(tuple);
            return;
        }

        //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> take(ArrayList tuple){
            List<ArrayList> res = read(tuple);
            //elementos da lista sao referencia ou e a lista mesmo?
            //podemos simplesmente remover ou temos de ir procurar o indice?
            //TODO remover primeiro indice de res
            tupleContainer.Remove(tuple);
            return res; 
        }

        public List<ArrayList> read(ArrayList tuple){
            List<ArrayList> res = new List<ArrayList>();
            Regex capital = new Regex(@"[A-Z]");
            //el = cada elemento dentro da array list
            foreach (ArrayList el in tupleContainer){
                if(el.Count != tuple.Count){
                    continue;
                }
                //sao do mesmo tamanho, vamos percorrer elemento a elemento nos 2
                for(int i = 0; i < tuple.Count; i++ ){

                    if (!((tuple[i].GetType() == null) || //pedido null devolve qualquer objecto
                          (tuple[i].GetType() == typeof(System.String)) ||
                          (el[i].GetType() == tuple[i].GetType())
                        )){
                        //se entrar aqui os tipos sao diferentes e o pedido nao e wildcard
                        //este break esta mal acho
                        break;
                    }
                    if(el[i].GetType() == typeof(System.String)) {
                        if (tuple[i] != null && capital.IsMatch(tuple[i].ToString())){ }//request is a object and we are seeing string
                        else if (!matchStrs(el[i], tuple[i])) {
                                break;
                        }
                    }
                    else {
                        if (tuple[i] == null) {}//here to keep null requests out of next if
                        //se for uma string sabemos que 
                        if (tuple[i].GetType() == typeof(System.String) && el[i].GetType().ToString() != tuple[i].ToString()) {
                            break;
                        }
                        else if (tuple[i].Equals(el[i])) {
                            break;
                        }
                    }
                }
                res.Add(el);
            }
            return res;
        }

        //tratar as wildcards
        //objetos agora sao diferentes de strings por isso nao e necessario tanta cena
        //TODO
        private bool matchStrs(object local, object request)
        {
            if (request.GetType() == typeof(System.String).GetType()) {
                if (request.GetType() == typeof(System.String)) {
                    return true;
                }
            }
            //Ver se o request e um object ou e uma string
            //secalhar tostring nao devolve a string guardada
            //TODO VERIFICAR ESTES CASTS, PODEM DAR BASTANTE MERDA
            string requeststr = (string)request;
            string localstr = (string)local;
            if (requeststr == "*") {
                if (local.GetType() == typeof(System.String)) {
                    return true;
                }
            }
            //a partir das ultimas duas verificacoes temos de ver as strings uma a uma
            if (requeststr.Contains("*")) {
                string regex = "";
                if(requeststr[0].ToString() == "*") {//s e o asterisco for o primeiro entao ta bem
                    //quero o resto da string menos o primeiro elemento
                    regex = ".*" + requeststr.Substring(1);
                }
                else {
                    //quero o resto da string menos o ultimo elemento
                    regex = "^" + requeststr.Substring(0, (requeststr.Length -1));
                }

                //TODO ver se funciona nao tenho tempo
                Regex wildcard = new Regex("@" + regex);

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
