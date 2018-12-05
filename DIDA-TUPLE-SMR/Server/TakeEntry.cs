using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    [Serializable]
    public class TakeEntry : Entry {

        public TakeEntry(TupleClass tuple, int term, int logIndex, string type) : base(tuple, term, logIndex, type) {

        }
    }
}
