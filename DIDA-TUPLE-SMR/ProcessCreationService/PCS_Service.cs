using CreationServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCreationService {
    class PCS_Service : I_PCS_Service {
        public void Crash(string processname) {
            throw new NotImplementedException();
        }

        public void CreateClient(string id, string URL, string script_file) {
            throw new NotImplementedException();
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            throw new NotImplementedException();
        }

        public void Freeze(string processname) {
            throw new NotImplementedException();
        }

        public void PrintStatus() {
            throw new NotImplementedException();
        }

        public void Unfreeze(string processname) {
            throw new NotImplementedException();
        }
    }
}
