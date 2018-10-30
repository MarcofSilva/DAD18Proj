using System;
using System.Collections.Generic;
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

            foreach  (string filename in args)
            {
                reader = File.OpenText(filename);
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split();
                    switch (items[0])
                    {
                        case "add":
                            throw new NotImplementedException();
                        case "read":
                            throw new NotImplementedException();
                        case "take":
                            throw new NotImplementedException();
                        case "wait":
                            //Sleep(items[1]);
                        case "begin-repeat":
                            throw new NotImplementedException();
                        case "end-repeat":
                            throw new NotImplementedException();
                        default:
                            throw new NotImplementedException();

                    }
                }
            }
        }
    }
}
