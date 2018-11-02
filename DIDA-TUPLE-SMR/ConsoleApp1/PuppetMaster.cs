using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1 {
    class PuppetMaster {

        private void configure(string key) {
            //TODO configure puppetmaster
        }


        private void createServer(string server_id, string serverURL, string min_delay, string max_Delay) {
            throw new NotImplementedException();
        }

        private void createClient(string client_id, string clientURL, string script_file) {
            throw new NotImplementedException();
        }

        private void executeCommand(string command) {
            string[] items = command.Split(' ');
            switch (items[0]) {
                case "Server":
                    createServer(items[1], items[2], items[3], items[4]);
                    break;
                case "Client":
                    createClient(items[1], items[2], items[3]);
                    break;
                case "Status":
                    break;
                case "Crash":
                    break;
                case "Freeze":
                    break;
                case "Unfreeze":
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
            Console.WriteLine("quit to stop...");
            PuppetMaster pMaster = new PuppetMaster();

            while (true) {
                string line = Console.ReadLine();
                if (line == "quit") {
                    break;
                }
                foreach (string key in ConfigurationManager.AppSettings.AllKeys) {
                    pMaster.configure(key);
                }
                pMaster.executeCommand(line);
            }
        }
    }
}
