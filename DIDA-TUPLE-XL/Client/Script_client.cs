using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Client
{
    class Script_Client
    {
        private API_XL _tupleSpaceAPI;
        private string defaultURL = "tcp://4.5.6.7:60001/C";

        public Script_Client(){
            _tupleSpaceAPI = new API_XL(defaultURL);
        }

        public Script_Client(string URL) {
            _tupleSpaceAPI = new API_XL(URL);
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

        private void executeOperation(string commandLine)
        {
            ArrayList tuple;
            ArrayList response;

            string[] commandItems = commandLine.Split(new char[] { ' ' }, 2);

            switch (commandItems[0])
            {
                case "add":
                    tuple = getTuple(commandItems[1]);
                    Console.WriteLine("Operation: " + commandLine + "\n");
                    _tupleSpaceAPI.Write(tuple);
                    break;

                case "read":
                    tuple = getTuple(commandItems[1]);
                    Console.WriteLine("Operation: " + commandLine);

                    response = _tupleSpaceAPI.Read(tuple);
                    Console.Write("Response: ");
                    if (response == null) {
                        Console.WriteLine("No match found\n");
                    }
                    else {
                        Console.WriteLine(response + "\n");
                    }

                    break;

                case "take":
                    tuple = getTuple(commandItems[1]);
                    Console.WriteLine("Operation: " + commandLine);

                    response = _tupleSpaceAPI.Take(tuple);
                    Console.Write("Response: ");
                    if (response == null) {
                        Console.WriteLine("No match found\n");
                    }
                    else {
                        Console.WriteLine(response + "\n");
                    }
                    break;

                case "wait":
                    Console.Write("wait" + commandItems[1]);
                    System.Threading.Thread.Sleep(int.Parse(commandItems[1]));
                    Console.WriteLine("Operation: " + commandLine + "\n");
                    break;
            }
        }

        private void executeScript(string scriptName)
        {
            StreamReader reader = null;

            try {
                reader = File.OpenText(scriptName);
            }
            catch (FileNotFoundException) {
                Console.WriteLine("File not found!");
                return;
            }

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
                            executeOperation(commandLine);
                        }
                        repeatIterations--;
                    }
                }
                else
                {
                    executeOperation(line);
                }
            }
            reader.Close();
        }

        static void Main(string[] args)
        {
            Script_Client client;
            if (args.Length == 0) {
                client = new Script_Client();
            }
            else {
                client = new Script_Client(args[0]);
                client.executeScript(args[1]);
                Console.WriteLine(args[1]);
            }

            while (true) {
                Console.WriteLine("Enter script(s) (Quit to stop)...");

                string line = Console.ReadLine();
                string[] commands = line.Split();

                if (commands[0].Equals("Quit") || commands[0].Equals("quit")) {
                    break;
                }

                foreach (string filename in commands) {
                    client.executeScript(filename);
                }
            }
        }
    }
}
