using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    public abstract class Entry {

        private TupleClass _tuple;
        private int _term;
        private int indexLog;
        private bool commited = false;

        public int Term { get => _term; set => _term = value; }
        public TupleClass Tuple { get => _tuple; set => _tuple = value; }
        public bool Commited { get => commited; set => commited = value; }
        public int IndexLog { get => indexLog; set => indexLog = value; }

        public Entry(TupleClass tuple, int term) {
            Term = term;
            Tuple = tuple;
        }

        public void commit() {
            commited = true;
        }

    }
}
