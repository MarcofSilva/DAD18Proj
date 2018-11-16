using CreationServiceLibrary;
using Client;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ProcessCreationService {
    class PCS_Service : MarshalByRefObject, I_PCS_Service {
        private PCS _pcs;
        private Dictionary<string, Script_Client> clients = new Dictionary<string, Script_Client>();
        private Dictionary<string, Server.Server> servers = new Dictionary<string, Server.Server>();

        public PCS_Service(PCS pcs) {
            _pcs = pcs;
        }

        public void CreateClient(string id, string URL, string script_file) {
            //Script_Client client = new Script_Client();
            //clients.Add(id, client);
            //client.executeScript(script_file);
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Creating server at port " + urlSplit[2]);
            Process proc = new Process();
            proc.StartInfo.FileName = "Server";
            proc.StartInfo.Arguments = URL;
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.Start();

        }

        public void Crash(string id) {
            Console.WriteLine("Crashing " + id);
            servers[id].crash();
        }

        public void Freeze(string id) {
            Console.WriteLine("Freeze!");
        }

        public void PrintStatus() {
            Console.WriteLine("hey");
        }

        public void Unfreeze(string processname) {
            throw new NotImplementedException();
        }
    }
}
