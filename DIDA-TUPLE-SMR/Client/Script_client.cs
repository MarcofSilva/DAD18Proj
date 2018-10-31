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
        private TupleSpaceAPI _tupleSpaceAPI;

        public Script_Client()
        {
            _tupleSpaceAPI = new TupleSpaceAPI();
        }

        private string ConstructString(string textToParse, ref int index)
        {
            string aux = "";
            index++;//TODO MARTELO AQUI PARA IGNORAR AS ASPAS

            //condicao do " esta aqui no caso de existirem aspas no meio, nao sei se e possivel
            for (; !(textToParse[index] == '"' && (textToParse[index + 1] == ',' || textToParse[index + 1] == '>')) ; index++)
            {
                aux += textToParse[index].ToString();
            }

            return aux;
        }
        private int ConstructInt(string textToParse, ref int index)
        {   
            string aux = "";
            for(; !(textToParse[index] == ',' || textToParse[index] == '>'); index++)
            {
                aux += textToParse[index].ToString();
            }
            return Int32.Parse(aux);
        }
        private string ConstructObject(string textToParse, ref int index)
        {
            //existem ) la dentro?
            string aux = "";
            
            for (; !(textToParse[index] == ')' && (textToParse[index + 1] == ',' || textToParse[index + 1] == '>')); index++)
            {
                aux += textToParse[index].ToString();
            }

            return aux + ")"; //TODO MARTELO
        }

        private ArrayList getTuple(string textToParse)
        {
            ArrayList res = new ArrayList();
            Regex numbers = new Regex(@"[0-9]");
            //se for preciso adicionar o espaco
            Regex noproblem = new Regex(@">,");
            for (int i = 0; i < textToParse.Length; i++)
            {
                //esta aqui a , por causa das condicoes de paragem e indices
                //TODO NAO ESTOU A VER QUANDO A LETRA CONSUMIDA FOI UM <, LOGO PODE DAR INDEX OUT OF RANGE
                if (noproblem.IsMatch(textToParse[i].ToString())) { continue; }
                if (textToParse[i] == '>') { break; }
                if (textToParse[i] == '"')
                {
                    res.Add(ConstructString(textToParse, ref i ));
                    continue;
                }
                if (numbers.IsMatch(textToParse[i].ToString()))
                {
                    res.Add(ConstructInt(textToParse, ref i));
                    continue;
                }
                else
                {
                    res.Add(ConstructObject(textToParse, ref i));
                    continue;
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
        }
    }
}
