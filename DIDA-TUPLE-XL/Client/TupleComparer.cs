using ClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client {
    class TupleComparer : IEqualityComparer<TupleClass> {
        public bool Equals(TupleClass x, TupleClass y) {
            return x.Equals(y);
        }

        public int GetHashCode(TupleClass obj) {
            return obj.GetHashCode();
        }
    }
}
