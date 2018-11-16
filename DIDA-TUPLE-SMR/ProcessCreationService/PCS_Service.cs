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
        private Dictionary<string, Process> processes = new Dictionary<string, Process>();

        public PCS_Service(PCS pcs) {
            _pcs = pcs;
        }

        public void CreateClient(string id, string URL, string script_file) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Creating client at port " + urlSplit[2]);
            Process proc = new Process();
            proc.StartInfo.FileName = "Client";
            proc.StartInfo.Arguments = URL + " " + script_file;
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.Start();
            processes.Add(id, proc);
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Creating server at port " + urlSplit[2]);
            Process proc = new Process();
            proc.StartInfo.FileName = "Server";
            proc.StartInfo.Arguments = URL;
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.Start();
            processes.Add(id, proc);

        }

        public void Crash(string id) {
            Console.WriteLine("Crashing " + id);
            processes[id].Kill();
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        public void Freeze(string id) {
            Console.WriteLine("Freezing " + id);
            Process proc = processes[id];
            ProcessThread p = proc.Threads[0];
            IntPtr ptrOpenThread = OpenThread(0x0002, false, (uint)p.Id);
            if (ptrOpenThread != null) {
                SuspendThread(ptrOpenThread);
            }

        }

        public void PrintStatus() {
            foreach (Process proc in processes.Values){
                Console.WriteLine(proc.Responding);
            }
        }

        public void Unfreeze(string id) {
            Console.WriteLine("Unfreezing " + id);
            Process proc = processes[id];
            ProcessThread p = proc.Threads[0];
            IntPtr ptrOpenThread = OpenThread(0x0002, false, (uint)p.Id);
            if (ptrOpenThread != null) {
                ResumeThread(ptrOpenThread);
            }

        }
    }
}
