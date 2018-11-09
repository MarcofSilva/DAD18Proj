using CreationServiceLibrary;
using Client;
using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService {
    class PCS_Service : MarshalByRefObject, I_PCS_Service {
        private PCS _pcs;
        private Dictionary<string, Script_Client> clients = new Dictionary<string, Script_Client>();

        public PCS_Service(PCS pcs) {
            _pcs = pcs;
        }

        public void Crash(string processname) {
            throw new NotImplementedException();
        }

        public void CreateClient(string id, string URL, string script_file) {
            Script_Client client = new Script_Client();
            clients.Add(id, client);
            client.executeScript(script_file);
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            Console.WriteLine("2");
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            int url;
            Int32.TryParse(urlSplit[2],out url);
            Console.WriteLine(url);
            Server.Server server = new Server.Server(url);
        }

        public void Freeze(string processname) {
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
