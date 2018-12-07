using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClassLibrary;
using System.Text.RegularExpressions;

namespace ClassLibrary {
    [Serializable]
    public class TupleClass {

        private ArrayList _tuple = new ArrayList();

        private int _size;

        public TupleClass() {
        }

        public TupleClass(string textToParse) {
            Regex noproblem = new Regex(@"[<>,]");
            for (int i = 0; i < textToParse.Length; i++) {
                if (noproblem.IsMatch(textToParse[i].ToString())) { continue; }
                if (textToParse[i] == '"' || textToParse[i] == '*') {
                    this.Add(ConstructString(textToParse, ref i));
                    continue;
                }
                else {
                    this.Add(ConstructObject(textToParse, ref i));
                }
            }
        }

        public ArrayList tuple {
            get {
                return _tuple;
            }

            set {
                _tuple = value;
            }
        }

        public int Size {
            get {
                return _size;
            }

            set {
                _size = value;
            }
        }

        public void Add(Object o) {
            _tuple.Add(o);
            _size += 1;
        }

        public override string ToString() {
            string acc = "<";
            for (int i = 0; i < _tuple.Count; i++) {
                if (i != 0) {
                    acc += ",";
                }
                if (_tuple[i] == null) {
                    acc += "null";
                }
                else if (_tuple[i].GetType() == typeof(System.String)) {
                    acc += "\"" + _tuple[i].ToString() + "\"";
                }
                else if (_tuple[i] == typeof(DADTestA)) {
                    acc += "DADTestA";
                }
                else if (_tuple[i] == typeof(DADTestB)) {
                    acc += "DADTestB";
                }
                else if (_tuple[i] == typeof(DADTestC)) {
                    acc += "DADTestC";
                }
                else {
                    acc += _tuple[i].ToString();
                }
            }
            acc += ">";
            return acc;
        }
        //Returns true if two tuples are the same and false otherwise
        public bool Equals(TupleClass tupler) {
            if (_size != tupler.Size) {
                return false;
            }
            for(int i =0; i < _size; i++) {
                ArrayList tuple = tupler.tuple;
                if (_tuple[i].GetType() != tuple[i].GetType()) {
                    return false;
                }
                if((_tuple[i].GetType() == typeof(System.String)) && (tuple[i].GetType() == typeof(System.String))) {
                    if ( !((string)_tuple[i]).Equals(((string)tuple[i])) ) {
                        return false;
                    }
                    continue;
                }
                else {
                    if (_tuple[i].GetType() == typeof(DADTestA) && tuple[i].GetType() == typeof(DADTestA)) {
                        DADTestA tuplei = (DADTestA)_tuple[i];
                        DADTestA eli = (DADTestA)tuple[i];
                        if (!tuplei.Equals(eli)) {
                            return false;
                        }
                    }
                    else if (_tuple[i].GetType() == typeof(DADTestB) && tuple[i].GetType() == typeof(DADTestB)) {
                        DADTestB tuplei = (DADTestB)_tuple[i];
                        DADTestB eli = (DADTestB)tuple[i];
                        if (!tuplei.Equals(eli)) {
                            return false;
                        }
                    }
                    else if (_tuple[i].GetType() == typeof(DADTestC) && tuple[i].GetType() == typeof(DADTestC)) {
                        DADTestC tuplei = (DADTestC)_tuple[i];
                        DADTestC eli = (DADTestC)tuple[i];
                        if (!tuplei.Equals(eli)) {
                            return false;
                        }
                    }
                    else {
                        return false;
                    }
                }
            }
            return true;
        }

        //returns true if two tuples match;
        //takes care of wildcards
        public bool Matches(TupleClass tupler) {
            if (_size != tupler.Size) {
                return false;
            }
            for (int i = 0; i < _size; i++) {
                ArrayList tuple = tupler.tuple;
                //if request is null and we are seeing object
                if (tuple[i] == null && _tuple[i].GetType() != typeof(System.String)) {
                    continue;
                }
                //if request is not null, they are either both strings or both types/objects, false otherwise
                if (tuple[i] != null && !((tuple[i].GetType() == typeof(System.String)) && (_tuple[i].GetType() == typeof(System.String)) ||
                                          (tuple[i].GetType() != typeof(System.String)) && (_tuple[i].GetType() != typeof(System.String)))) {
                    return false;
                }
                //if one is string the other is string
                if (_tuple[i].GetType() == typeof(System.String)) {
                    if (!matchStrs(_tuple[i], tuple[i])) {
                        return false;
                    }
                }
                else if (tuple[i] == typeof(DADTestA) && _tuple[i].GetType() == typeof(DADTestA)) {
                }
                else if (tuple[i] == typeof(DADTestB) && _tuple[i].GetType() == typeof(DADTestB)) {
                }
                else if (tuple[i] == typeof(DADTestC) && _tuple[i].GetType() == typeof(DADTestC)) {
                }
                else if (tuple[i].GetType() == typeof(DADTestA) && _tuple[i].GetType() == typeof(DADTestA)) {
                    DADTestA tuplei = (DADTestA)tuple[i];
                    DADTestA eli = (DADTestA)_tuple[i];
                    if (!tuplei.Equals(eli)) {
                        return false;
                    }
                }
                else if (tuple[i].GetType() == typeof(DADTestB) && _tuple[i].GetType() == typeof(DADTestB)) {
                    DADTestB tuplei = (DADTestB)tuple[i];
                    DADTestB eli = (DADTestB)_tuple[i];
                    if (!tuplei.Equals(eli)) {
                        return false;
                    }
                }
                else if (tuple[i].GetType() == typeof(DADTestC) && _tuple[i].GetType() == typeof(DADTestC)) {
                    DADTestC tuplei = (DADTestC)tuple[i];
                    DADTestC eli = (DADTestC)_tuple[i];
                    if (!tuplei.Equals(eli)) {
                        return false;
                    }
                }
                else return false;
            }
            return true;
        }

        private bool matchStrs(object local, object request) {
            string requeststr = (string)request;
            string localstr = (string)local;
            if (requeststr == "*") {
                return true;
            }
            if (requeststr.Contains("*")) {
                string regex = "";
                if (requeststr[0].ToString() == "*") {
                    regex = ".*" + requeststr.Substring(1) + "$";
                }
                else {
                    regex = "^" + requeststr.Substring(0, (requeststr.Length - 1)) + ".*";
                }
                Regex wildcard = new Regex(regex);
                if (wildcard.IsMatch(localstr)) {
                    return true;
                }
            }
            if (requeststr == localstr) {
                return true;
            }
            return false;
        }

        private string ConstructString(string textToParse, ref int index){
            string aux = "";
            if (textToParse[index] == '*') {
                aux += textToParse[index].ToString();
            }
            else{
                index++;
            }

            for (; !(textToParse[index+1] == ',' || textToParse[index+1] == '>') ; index++){
                aux += textToParse[index].ToString();
            }
            return aux;
        }

        private Type ConstructType(string textToParse) {
            switch (textToParse) {
                case "DADTestA":
                    return typeof(DADTestA);
                case "DADTestB":
                    return typeof(DADTestB);
                case "DADTestC":
                    return typeof(DADTestC);
            }
            return null;
        }

        private Object ConstructObject(string textToParse, ref int index) {
            Regex ints = new Regex(@"^[0-9]+");
            Regex parenthesis = new Regex(@"[(]");
            string aux = "";
            string name = "";
            ArrayList arguments = new ArrayList();
            int auxint = index;
            string auxstr = "";
            for (; !(textToParse[auxint] == ',' || textToParse[auxint] == '>'); auxint++) {
                auxstr += textToParse[auxint].ToString();
            }
            if (!parenthesis.IsMatch(auxstr)) {
                index = auxint;
                if (auxstr == "null") {
                    return null;
                }
                return ConstructType(auxstr);
            }

            for (; !(textToParse[index - 1] == ')' && (textToParse[index] == ',' || textToParse[index] == '>')); index++) {

                if (textToParse[index] == '(') {
                    name = aux;
                    aux = "";
                    continue;
                }
                if ((textToParse[index] == ',' || textToParse[index] == ')') && aux.Length > 0) {
                    if (ints.IsMatch(aux)) {
                        int a;
                        if (Int32.TryParse(aux, out a)) {
                            arguments.Add(a);
                        }
                    }
                    else {
                        arguments.Add(aux);
                    }
                    aux = "";
                    continue;
                }
                if (textToParse[index].ToString() != "\"") {
                    aux += textToParse[index].ToString();
                }
            }
            switch (name) {
                case "DADTestA":
                    return new DADTestA((int)arguments[0], (string)arguments[1]);
                case "DADTestB":
                    return new DADTestB((int)arguments[0], (string)arguments[1], (int)arguments[2]);
                case "DADTestC":
                    return new DADTestC((int)arguments[0], (string)arguments[1], (string)arguments[2]);
            }
            return null;
        }
    }
}
