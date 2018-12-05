using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    [Serializable]
    public class TakeEntry : Entry {

        public TakeEntry(TupleClass tuple, int term, int logIndex) : base(tuple, term, logIndex) {

        }
    }
}
