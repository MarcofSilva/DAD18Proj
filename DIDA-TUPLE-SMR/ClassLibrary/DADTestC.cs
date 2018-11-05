using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary {
    [Serializable]
    public class DADTestC {
        public int i1;
        public string s1;
        public string s2;

        public DADTestC(int pi1, string ps1, string ps2) {
            i1 = pi1;
            s1 = ps1;
            s2 = ps2;
        }

        public bool Equals(DADTestC o) {
            if (o == null) {
                return false;
            }
            else {
                return ((this.i1 == o.i1) && (this.s1.Equals(o.s1)) && (this.s2.Equals(o.s2)));
            }
        }
    }
}
