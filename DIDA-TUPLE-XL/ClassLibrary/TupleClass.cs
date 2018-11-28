using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;

namespace ClassLibrary {
    [Serializable]
    public class TupleClass {

        //List<?> tuple = new List<?>();        
        public ArrayList _tuple = new ArrayList();
        private int _size;

        public int Size {
            get {
                return _size;
            }

            set {
                _size = value;
            }
        }

        //if 2 tuples are the same
        public bool Equals(TupleClass tuple) {
            if (_size != tuple.Size) {

            }

            return false;
        }


        //if 2 tuples match, takes care of wildcards
        public bool Matches(TupleClass obj) {

            return false;
        }

        public void Add(Object o) {
            _tuple.Add(o);
            _size += 1;
        }
    }
}
