using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientLibrary;

namespace Client
{
    class Script_Client
    {
        private API_SMR _tupleSpaceAPI;

        public Script_Client() {
            _tupleSpaceAPI = new API_SMR();
        }

        private string ConstructString(string textToParse, ref int index) {
            string aux = "";
            index++;//TODO incrementing index to ignore first quote

            for (; !(textToParse[index+1] == ',' || textToParse[index+1] == '>') ; index++) {
                aux += textToParse[index].ToString();
            }

            return aux;
        }
        private int ConstructInt(string textToParse, ref int index) {
            string aux = "";
            for(; !(textToParse[index] == ',' || textToParse[index] == '>'); index++) {
                aux += textToParse[index].ToString();
            }
            return Int32.Parse(aux);
        }

        private Object ConstructType(string textToParse, ref int index) {
            string aux = "";
            Object res = null;
            for (; !(textToParse[index] == ',' || textToParse[index] == '>'); index++) {
                aux += textToParse[index].ToString();
            }
            switch (aux) {
                //TODO adicionar todos os outros tipos possíveis
                case "Integer":
                    res = Activator.CreateInstance(typeof(System.Int32));
                    break;
                case "Boolean":
                    res = Activator.CreateInstance(typeof(System.Boolean));
                    break;
            }
            return res;
        }

        private string ConstructObject(string textToParse, ref int index, ref bool isObject)
        {
            //TODO falta poder aceitar nome de um data type e null
            string aux = "";
            int starting_index = index;
            
            for (; !(textToParse[index-1] == ')' && textToParse[index] == ',' || textToParse[index] == '>'); index++) {
                if (textToParse[index] == '(') {
                    isObject = true;
                }
                if (textToParse[index] == ',' && !isObject) {
                    break;
                }
                aux += textToParse[index].ToString();
            }
            if (aux == "null") {
                aux = null;
            }
            if (!isObject) {
                index = starting_index;
            }
            return aux;
        }

        private ArrayList getTuple(string textToParse) {
            ArrayList res = new ArrayList();
            Regex numbers = new Regex(@"[0-9]");
            //se for preciso adicionar o espaco
            Regex noproblem = new Regex(@"[<>,]");
            bool isObject = false;
            for (int i = 0; i < textToParse.Length; i++) {
                if (noproblem.IsMatch(textToParse[i].ToString())) { continue; }
                if (textToParse[i] == '"') {
                    res.Add(ConstructString(textToParse, ref i ));
                    continue;
                }
                if (numbers.IsMatch(textToParse[i].ToString())) {
                    res.Add(ConstructInt(textToParse, ref i));
                    continue;
                }
                else {
                    string aux = ConstructObject(textToParse, ref i, ref isObject);
                    if (!isObject) {
                        res.Add(ConstructType(textToParse, ref i));
                    }
                    else {
                        res.Add(aux);
                    }
                    continue;
                }
            }
            return res;
        }

        private void executeOperation(string[] commandItems) {
            ArrayList tuple;

            switch (commandItems[0]) {
                case "add":
                    tuple = getTuple(commandItems[1]);
                    _tupleSpaceAPI.Write(tuple);
                    break;

                case "read":
                    tuple = getTuple(commandItems[1]);
                    _tupleSpaceAPI.Read(tuple);
                    break;

                case "take":
                    tuple = getTuple(commandItems[1]);
                    _tupleSpaceAPI.Take(tuple);
                    break;

                case "wait":
                    Console.WriteLine("wait" + commandItems[1]);
                    System.Threading.Thread.Sleep(int.Parse(commandItems[1]));
                    break;
            }
        }

        private void executeScript(string scriptName) {
            StreamReader reader = File.OpenText(scriptName);
            string line;

            //Repeat auxs
            int repeatIterations = 0;
            ArrayList commandsInRepeat = new ArrayList();

            while ((line = reader.ReadLine()) != null) {
                string[] items = line.Split(new char[] { ' ' }, 2);

                if (items[0].Equals("begin-repeat")) {
                    repeatIterations = int.Parse(items[1]);
                    while (!(line = reader.ReadLine()).Equals("end-repeat")) {
                        commandsInRepeat.Add(line);
                    }
                    while (repeatIterations > 0) {
                        foreach (string commandLine in commandsInRepeat) {
                            string[] commandItems = commandLine.Split(new char[] { ' ' }, 2);
                            executeOperation(commandItems);
                        }
                        repeatIterations--;
                    }
                }
                else {
                    executeOperation(items);
                }
            }
            reader.Close();
        }

        static void Main(string[] args) {
            Script_Client client = new Script_Client();

            foreach (string filename in args) {
                client.executeScript(filename);
            }

            Console.WriteLine("Enter to stop...");
            Console.ReadLine();
        }
    }
}
