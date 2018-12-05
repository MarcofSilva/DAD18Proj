using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server {
    [Serializable]
    public class EntryPacket {

        private List<Entry> _entrys = new List<Entry>();
        private int count;
        public List<Entry> Entrys { get => _entrys; set => _entrys = value; }
        public int Count { get => count; set => count = value; }

        public EntryPacket() {
            Count = 0;
        }
        public EntryPacket(List<Entry> entrys) {
            Entrys = entrys;
            Count = entrys.Count;
        }


    }
}
