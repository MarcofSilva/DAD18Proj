using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientLibrary;
using ClassLibrary;

namespace Client
{
    class Script_Client
    {
        private API_SMR _tupleSpaceAPI;

        public Script_Client(){
            _tupleSpaceAPI = new API_SMR();
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
                //TODO adicionar todos os outros tipos possíveis
                case "DADTestA":
                    return typeof(DADTestA);
                case "DADTestB":
                    return typeof(DADTestB);
                case "DADTestC":
                    return typeof(DADTestC);
            }
            //Careful default is null, existe mais alguma coisa para alem de Int e String?
            return null;
        }

        private Object ConstructObject(string textToParse, ref int index){
            //TODO falta poder aceitar nome de um data type e null
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
            //TODO MARTELOZAO
            if (!parenthesis.IsMatch(auxstr)) {
                index = auxint;
                if (auxstr == "null") {
                    return null;
                }
                return ConstructType(auxstr);
            }

            for (; !( textToParse[index-1] == ')' && (textToParse[index] == ',' || textToParse[index] == '>') ); index++){
                
                if (textToParse[index] == '('){
                    name = aux;
                    aux = "";
                    continue;
                }
                if ( (textToParse[index] == ',' || textToParse[index] == ')') && aux.Length > 0) {
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
                    //Console.WriteLine(((int)arguments[0]).ToString() + " " + ((string)arguments[1]).ToString());
                    return new DADTestA((int)arguments[0], (string)arguments[1]);
                case "DADTestB":
                    //Console.WriteLine(((int)arguments[0]).ToString() + " " + ((string)arguments[1]).ToString() + " " + ((int)arguments[2]).ToString());
                    return new DADTestB((int)arguments[0], (string)arguments[1], (int)arguments[2]);
                case "DADTestC":
                    //Console.WriteLine(((int)arguments[0]).ToString() + " " + ((string)arguments[1]).ToString() + " " + ((string)arguments[2]).ToString());
                    return new DADTestC((int)arguments[0], (string)arguments[1], (string)arguments[2]);
            }
            //Console.WriteLine(name);
            //Activator.CreateInstance(name, arguments);
            return null;
        }

        private ArrayList getTuple(string textToParse){
            ArrayList res = new ArrayList();
            //se for preciso adicionar o espaco
            Regex noproblem = new Regex(@"[<>,]");
            for (int i = 0; i < textToParse.Length; i++)
            {
                if (noproblem.IsMatch(textToParse[i].ToString())) { continue; }
                if (textToParse[i] == '"'|| textToParse[i] == '*') {
                    res.Add(ConstructString(textToParse, ref i ));
                    continue;
                }
                else{
                    res.Add(ConstructObject(textToParse, ref i));
                }
            }
            return res;
        }

        private void executeOperation(string[] commandItems)
        {
            ArrayList tuple;

            switch (commandItems[0])
            {
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
                    Console.Write("wait" + commandItems[1]);
                    System.Threading.Thread.Sleep(int.Parse(commandItems[1]));
                    break;
            }
        }

        private void executeScript(string scriptName)
        {
            StreamReader reader = File.OpenText(scriptName);
            string line;

            //Repeat auxs
            int repeatIterations = 0;
            ArrayList commandsInRepeat = new ArrayList();

            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split(new char[] { ' ' }, 2);

                if (items[0].Equals("begin-repeat"))
                {
                    repeatIterations = int.Parse(items[1]);
                    while (!(line = reader.ReadLine()).Equals("end-repeat"))
                    {
                        commandsInRepeat.Add(line);
                    }
                    while (repeatIterations > 0)
                    {
                        foreach (string commandLine in commandsInRepeat)
                        {
                            string[] commandItems = commandLine.Split(new char[] { ' ' }, 2);
                            executeOperation(commandItems);
                        }
                        repeatIterations--;
                    }
                }
                else
                {
                    executeOperation(items);
                }
            }
            reader.Close();
        }

        static void Main(string[] args)
        {
            Script_Client client = new Script_Client();
            foreach (string filename in args)
            {
                client.executeScript(filename);
            }
            Console.WriteLine("Enter to stop...");
            Console.ReadLine();
        }
    }
}
