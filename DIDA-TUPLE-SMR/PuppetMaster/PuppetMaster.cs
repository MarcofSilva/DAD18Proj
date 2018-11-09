using CreationServiceLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace PuppetMaster {
    class PuppetMaster {

        private TcpChannel _channel;
        private I_PCS_Service pcs;
        private Dictionary<string, I_PCS_Service> pcsList = new Dictionary<string, I_PCS_Service>(); //<ip:object>
        private Dictionary<string, I_PCS_Service> idToPcs = new Dictionary<string, I_PCS_Service>(); //<processid:object>

        public PuppetMaster() {
            _channel = new TcpChannel(10001);
            ChannelServices.RegisterChannel(_channel, false);
        }
        //TODO otimizacao: localhost nao precisa de ter PCS
        private void configure() {
            foreach (string url in ConfigurationManager.AppSettings) {
                string[] urlSplit = url.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                pcsList.Add(urlSplit[1], (I_PCS_Service)Activator.GetObject(typeof(I_PCS_Service), url));
            }
        }

        private void executeCommand(string command) {
            string[] items = command.Split(' ');
            switch (items[0]) {
                case "Server":
                    int n3, n4;
                    Int32.TryParse(items[3], out n3);
                    Int32.TryParse(items[4], out n4);
                    string[] urlServer = items[2].Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    idToPcs.Add(items[1], pcsList[urlServer[1]]);
                    Console.WriteLine("1");
                    pcsList[urlServer[1]].CreateServer(items[1], items[2], n3, n4);
                    Console.WriteLine("-");

                    break;
                case "Client":
                    string[] urlClient = items[2].Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    idToPcs.Add(items[1], pcsList[urlClient[1]]);
                    pcsList[urlClient[1]].CreateClient(items[1], items[2], items[3]);
                    break;
                case "Status":
                    ICollection allPcs = pcsList.Values;
                    foreach (I_PCS_Service pcs in allPcs) {
                        pcs.PrintStatus();
                    }
                    break;
                case "Crash":
                    idToPcs[items[1]].Crash(items[1]);
                    break;
                case "Freeze":
                    idToPcs[items[1]].Freeze(items[1]);
                    break;
                case "Unfreeze":
                    idToPcs[items[1]].Unfreeze(items[1]);
                    break;
                case "Wait":
                    Console.Write("wait" + items[1]);
                    System.Threading.Thread.Sleep(int.Parse(items[1]));
                    break;
                default:    //script
                    break;
            }
        }


        static void Main(string[] args) {
            Console.WriteLine("Quit to stop...");
            PuppetMaster pMaster = new PuppetMaster();
            pMaster.configure();

            while (true) {
                string line = Console.ReadLine();
                if (line.Equals("Quit") || line.Equals("quit")) {
                    break;
                }
                pMaster.executeCommand(line);
            }
            Console.WriteLine("GoodBye...");
        }
    }
}
