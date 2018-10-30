using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Script_Client
    {
        static void Main(string[] args)
        {
            StreamReader reader;

            foreach (string filename in args)
            {
                reader = File.OpenText(filename);
                string line;
                bool repeat = false;
                int times = 0;
                ArrayList accumulator = new ArrayList();

                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(' ');
                    switch (items[0])
                    {
                        case "add":
                            if (repeat)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                accumulator.Add(line);
                            }
                            throw new NotImplementedException();

                        case "read":
                            if (repeat)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                accumulator.Add(items[0] + items[1]);
                            }
                            throw new NotImplementedException();

                        case "take":
                            if (repeat)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                accumulator.Add(items[0] + items[1]);
                            }
                            throw new NotImplementedException();

                        case "wait":
                            if (repeat)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                accumulator.Add(items[0] + items[1]);
                            }
                            throw new NotImplementedException();
                        case "begin-repeat":
                            repeat = true;
                            times = Int32.Parse(items[1]);
                            throw new NotImplementedException();

                        case "end-repeat":
                            repeat = false;
                            while (times > 0)
                            {
                                //switch vai para funcao
                                //chama a funcao com cada coisa
                            }
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();

                    }
                }
            }
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
                if (noproblem.IsMatch(textToParse[i].ToString())){ continue; }
                if (textToParse[i] == '>') { break; }
                if (textToParse[i] == '"')
                {
                    res.Add(ConstructString(textToParse, ref i));
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
            
        private string ConstructString(string textToParse, ref int index)
        {
            string aux = "";
            index++;//TODO MARTELO AQUI PARA IGNORAR AS ASPAS

            //condicao do " esta aqui no caso de existirem aspas no meio, nao sei se e possivel
            for (; textToParse[index] == '"' && (textToParse[index + 1] == ',' || textToParse[index + 1] == '>') ; index++)
            {
                aux += textToParse[index].ToString();
            }

            return aux;
        }
        private int ConstructInt(string textToParse, ref int index)
        {   
            string aux = "";
            for(; textToParse[index] == ',' || textToParse[index] == '>'; index++)
            {
                aux += textToParse[index].ToString();
            }
            return Int32.Parse(aux);
        }
        private string ConstructObject(string textToParse, ref int index)
        {
            //existem ) la dentro?
            string aux = "";
            
            for (; textToParse[index] == ')' && (textToParse[index + 1] == ',' || textToParse[index + 1] == '>'); index++)
            {
                aux += textToParse[index].ToString();
            }

            return aux + ")"; //TODO MARTELO
        }
    }
}
