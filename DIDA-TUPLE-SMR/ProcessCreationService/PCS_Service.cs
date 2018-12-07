using CreationServiceLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using RemoteServicesLibrary;

namespace ProcessCreationService {
    class PCS_Service : MarshalByRefObject, I_PCS_Service {
        private PCS _pcs;
        private Dictionary<string, Process> processes = new Dictionary<string, Process>();
        private Dictionary<string, string> serverUrl = new Dictionary<string, string>();
        private Dictionary<string, string> clientUrl = new Dictionary<string, string>();

        public PCS_Service(PCS pcs) {
            _pcs = pcs;
        }

        public void CreateClient(string id, string URL, string script_file) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Creating client at port " + urlSplit[2]);
            Process proc = new Process();
            proc.StartInfo.FileName = "..\\..\\..\\Client\\bin\\Debug\\Client";
            proc.StartInfo.Arguments = URL + " " + script_file;
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.Start();
            processes.Add(id, proc);
            clientUrl.Add(id, URL);
        }

        public void CreateServer(string id, string URL, int min_delay, int max_delay) {
            string[] urlSplit = URL.Split(new Char[] { '/', ':' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Creating server at port " + urlSplit[2]);
            Process proc = new Process();
            proc.StartInfo.FileName = "..\\..\\..\\Server\\bin\\Debug\\Server";
            proc.StartInfo.Arguments = URL + " " + min_delay.ToString() + " " + max_delay.ToString();
            Console.WriteLine(proc.StartInfo.Arguments);
            proc.Start();
            processes.Add(id, proc);
            serverUrl.Add(id, URL);

        }

        public void Crash(string id) {
            Console.WriteLine("Crashing " + id);
            processes[id].Kill();
            if (serverUrl.ContainsKey(id)) serverUrl.Remove(id);
            if (clientUrl.ContainsKey(id)) clientUrl.Remove(id);
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern uint ResumeThread(IntPtr hThread);

        public void Freeze(string id) {
            Console.WriteLine("Freezing " + id);
            if (serverUrl.ContainsKey(id)) {
                IServerService i = (IServerService)Activator.GetObject(typeof(IServerService), serverUrl[id]);
                i.Freeze();
            }
            else if (clientUrl.ContainsKey(id)) {
                IClientService i = (IClientService)Activator.GetObject(typeof(IClientService), clientUrl[id]);
                i.Freeze();
            }
            Console.WriteLine(id.ToString() + " frozen");
        }

        public void PrintStatus() {
            foreach (Process proc in processes.Values){
                Console.WriteLine(proc.Responding);
            }
        }

        public void Unfreeze(string id) {
            Console.WriteLine("Unfreezing " + id);
            if (serverUrl.ContainsKey(id)) {
                IServerService i = (IServerService)Activator.GetObject(typeof(IServerService), serverUrl[id]);
                i.Unfreeze();
            }
            else if (clientUrl.ContainsKey(id)) {
                IClientService i = (IClientService)Activator.GetObject(typeof(IClientService), clientUrl[id]);
                i.Unfreeze();
            }
        }
    }
}
