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
            tupleContainer.Add(tuple);
            return;
        }

                //devolve arraylist vazia/1 elemento ou varios
        public List<ArrayList> take(ArrayList tuple){
            List<ArrayList> res = read(tuple);
            //elementos da lista sao referencia ou e a lista mesmo?
            //podemos simplesmente remover ou temos de ir procurar o indice?
            //TODO
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
                    //TODO nao considera objetos e null se for possivel escrever integer no tuplo, isto nao esta a fazer

                    //tuple = pedido
                    //el = tuplo do container que estamos a ver
                    //wc = wild card

                    if (!( (el[i].GetType() == typeof(System.Int32) && tuple[i].GetType() == typeof(System.Int32).GetType()) || //um int outro integer wc 
                           (el[i].GetType() == typeof(System.String) && tuple[i].GetType() == typeof(System.String).GetType()) || //um str outro string wc
                           (tuple[i].GetType() == null) || //pedido null devolve qualquer objecto
                           (el[i].GetType() == tuple[i].GetType()) // os 2 ints, 2 strings, 2 do mesmo objecto
                        )){
                        //se entrar aqui os tipos sao diferentes e o pedido nao e wildcard
                        break;
                    }
                    if (el[i].GetType() == typeof(System.Int32) && tuple[i].GetType() != null) {
                        if(!matchInts(el[i], tuple[i])) {
                            break;
                        }
                    }
                    else {
                        if (!capital.IsMatch(el[i].ToString()) && //o que estou a ver nao e um objeto
                            !capital.IsMatch(tuple[i].ToString()) && //o pedido nao e um objeto
                            el[i] != null && //o  a ver nao e null
                            tuple[i] != null && //o pedido nao e null
                            el[i].GetType() == typeof(System.String) && //o que estou a ver e uma string
                            //o pedido e uma string                             ou um type string
                            (tuple[i].GetType() == typeof(System.String) || tuple[i].GetType() == typeof(System.String).GetType())
                            ){
                            if (!matchStrs(el[i], tuple[i])) {
                                break;
                            }
                        }
                        else {
                            if (!matchObjs(el[i], tuple[i])) {
                                break;
                            }
                        }
                    }
                }
                res.Add(el);
            }
            return res;
        }

        //ver se o pedido e wild card e devolver todoas
        //ver se os dois inteiros sao iguais
        private bool matchInts(object local, object request)
        {
            if(request.GetType() == typeof(System.Int32).GetType()) {
                if (request.GetType() == typeof(System.Int32)) {
                    return true;
                }
            }
            else {
                return local == request;
            }
            return false;
        }
        //tratar as wildcards
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

        //TODO
        //IDEIAAAAA no interpretador um objeto em vez de vir em string vem da forma arraylist
        //primeiro elemento do array e o nome do objeto em string
        //o resto dos elementos sao elementos que o construtor recebe
        //ver se os argumentos dos construtores sao iguais
        //ver se o pedido e null
        private bool matchObjs(object local, object request)
        {
            if (request == null) {
                return true;
            }
            //ver construtores iguais
            return false;
        }
        static void Main(string[] args){
            Server server = new Server();
        }
    }
}
