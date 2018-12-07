using CreationServiceLibrary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PuppetMaster {
    class PuppetMaster {

        private const int PUPPETMASTER_DEFAULT_PORT = 10001;

        private TcpChannel _channel;
        private Dictionary<string, I_PCS_Service> pcsList = new Dictionary<string, I_PCS_Service>(); //<ip:object>
        private Dictionary<string, I_PCS_Service> idToPcs = new Dictionary<string, I_PCS_Service>(); //<processid:object>

        public delegate void createServerDelegate(String[] items);
        public delegate void createClientDelegate(String[] items);
        public delegate void statusDelegate();
        public delegate void printStatusDelegate();
        public delegate void crashDelegate(String id);
        public delegate void freezeDelegate(String id);
        public delegate void unfreezeDelegate(String id);

        public PuppetMaster() {
            _channel = new TcpChannel(PUPPETMASTER_DEFAULT_PORT);
            ChannelServices.RegisterChannel(_channel, false);
            configure();
        }
        private void configure() {
            foreach (string url in ConfigurationManager.AppSettings) {
                string[] urlSplit = url.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
                pcsList.Add(urlSplit[1], (I_PCS_Service)Activator.GetObject(typeof(I_PCS_Service), url));
            }
        }

        private void createServer(String[] items) {
            int n3, n4;
            Int32.TryParse(items[3], out n3);
            Int32.TryParse(items[4], out n4);
            string[] urlServer = items[2].Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            idToPcs.Add(items[1], pcsList[urlServer[1]]);
            pcsList[urlServer[1]].CreateServer(items[1], items[2], n3, n4);
        }

        private void createClient(String[] items) {
            string[] urlClient = items[2].Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            idToPcs.Add(items[1], pcsList[urlClient[1]]);
            pcsList[urlClient[1]].CreateClient(items[1], items[2], items[3]);
        }

        private void status() {
            ICollection allPcs = pcsList.Values;
            foreach (I_PCS_Service pcs in allPcs) {
                printStatusDelegate printStatusDel = new printStatusDelegate(pcs.PrintStatus);
                printStatusDel.BeginInvoke(null, null);
            }
        }

        private void crash(String id) {
            idToPcs[id].Crash(id);
        }

        private void freeze(String id) {
            idToPcs[id].Freeze(id);
        }

        private void unfreeze(String id) {
            idToPcs[id].Unfreeze(id);
        }

            private void executeCommand(string command) {
            string[] items = command.Split(' ');
            switch (items[0]) {
                case "Server":
                    createServerDelegate createServerDel = new createServerDelegate(createServer);
                    createServerDel.BeginInvoke(items, null, null);
                    break;
                case "Client":
                    createClientDelegate createClientDel = new createClientDelegate(createClient);
                    createClientDel.BeginInvoke(items, null, null);
                    break;
                case "Status":
                    statusDelegate statusDel = new statusDelegate(status);
                    statusDel.BeginInvoke(null, null);
                    break;
                case "Crash":
                    crashDelegate crashDel = new crashDelegate(crash);
                    crashDel.BeginInvoke(items[1], null, null);
                    break;
                case "Freeze":
                    freezeDelegate freezeDel = new freezeDelegate(freeze);
                    freezeDel.BeginInvoke(items[1], null, null);
                    break;
                case "Unfreeze":
                    unfreezeDelegate unfreezeDel = new unfreezeDelegate(unfreeze);
                    unfreezeDel.BeginInvoke(items[1], null, null);
                    break;
                case "Wait":
                    Console.Write("wait" + items[1]);
                    System.Threading.Thread.Sleep(int.Parse(items[1]));
                    break;
                default:
                    executeScript(items);
                    break;
            }
        }

        private void executeScript(string[] scriptArgs) {
            String scriptName = scriptArgs[0];

            StreamReader reader = null;
            try {
                reader = File.OpenText(scriptName);
            } catch (FileNotFoundException) {
                Console.WriteLine("File not found!");
                return;
            }

            string line;

            if(scriptArgs.Length == 1) {
                while ((line = reader.ReadLine()) != null) {
                    Console.WriteLine(line);
                    executeCommand(line);
                }
            }
            else if(scriptArgs.Length == 2 && scriptArgs[1].Equals("s")) {
                while ((line = reader.ReadLine()) != null) {
                    Console.WriteLine("Enter for next step");
                    Console.ReadLine();
                    Console.WriteLine(line);
                    executeCommand(line);
                }
            }
            else {
                Console.WriteLine("Execution mode unrecognized");
            }
            reader.Close();
        }


        static void Main(string[] args) {
            PuppetMaster pMaster = new PuppetMaster();

            if (args.Length != 0) {
                pMaster.executeScript(args);
            }

            Console.WriteLine("Possible inputs:\n- <command> ;\n" +
                                                 "- <scriptname> (run commands sequentially) or\n" +
                                                 "- <scriptname> s (run commands step by step) or ;\n" +
                                                 "- quit to stop...");

            while (true) {
                string line = Console.ReadLine();
                if (line.Equals("Quit") || line.Equals("quit")) {
                    break;
                }
                else if (line.Length > 0) {
                    pMaster.executeCommand(line);
                }
            }
            Console.WriteLine("GoodBye...");
        }
    }
}
