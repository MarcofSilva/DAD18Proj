using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    [Serializable]
    public abstract class Entry {

        private TupleClass _tuple;
        private int _term;
        private int logIndex;
        private string type;

        public int Term { get => _term; set => _term = value; }
        public TupleClass Tuple { get => _tuple; set => _tuple = value; }
        public int LogIndex { get => logIndex; set => logIndex = value; }
        public string Type { get => type; set => type = value; }

        public Entry(TupleClass tuple, int term, int logIndex, string type) {
            Term = term;
            Tuple = tuple;
            LogIndex = logIndex;
            Type = type;
        }

        public override string ToString() {
            return "tuple: " + Tuple.ToString() +" ;type; "  +type + " ;term: " + Term + " ;logIndex: " + LogIndex;
        }
    }
}
