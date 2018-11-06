using CreationServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService {
    class PCS_Service : MarshalByRefObject, I_PCS_Service {
        private PCS _pcs;

        public PCS_Service(PCS pcs) {
            _pcs = pcs;
        }

        public void Crash(string processname) {
            throw new NotImplementedException();
        }

        public void CreateClient(string id, string URL, string script_file) {
            throw new NotImplementedException();
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            Console.WriteLine(id);
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
