using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Server {
    [Serializable]
    public class WriteEntry : Entry {

        public WriteEntry(TupleClass tuple, int term, int logIndex) : base(tuple, term, logIndex) {

        }
    }
}
