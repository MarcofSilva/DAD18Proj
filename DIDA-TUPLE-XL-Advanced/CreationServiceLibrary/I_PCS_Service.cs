using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationServiceLibrary {
    public interface I_PCS_Service {

        void Crash(string processname);

        void CreateServer(string id, string URL, int min_delay, int max_delay);

        void CreateClient(string id, string URL, string script_file);

        void Freeze(string processname);

        void PrintStatus();

        void Unfreeze(string processname);
    }
}
