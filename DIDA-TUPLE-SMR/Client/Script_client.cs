﻿using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClassLibrary;

namespace Client
{
    public class Script_Client
    {
        private API_SMR _tupleSpaceAPI;
        private string defaultURL = "tcp://localhost:8080/C";

        public Script_Client() {
            _tupleSpaceAPI = new API_SMR(defaultURL);
        }

        public Script_Client(string URL){
            _tupleSpaceAPI = new API_SMR(URL);
        }

        //public?
        private void executeOperation(string commandLine) {
            TupleClass tuple;
            TupleClass response;

            string[] commandItems = commandLine.Split(new char[] { ' ' }, 2);

            switch (commandItems[0]) {
                case "add":
                    tuple = new TupleClass(commandItems[1]);
                    Console.WriteLine("Operation: " + commandLine + "\n");
                    _tupleSpaceAPI.write(tuple);

                    break;

                case "read":
                    tuple = new TupleClass(commandItems[1]);

                    Console.WriteLine("Operation: " + commandLine);

                    response = _tupleSpaceAPI.read(tuple);
                    Console.Write("Response: ");
                    if (response.Size == 0) {
                        Console.WriteLine("No match found\n");
                    }
                    else {
                        Console.WriteLine(response.ToString() + "\n");
                    }

                    break;

                case "take":
                    tuple = new TupleClass(commandItems[1]);
                    Console.WriteLine("Operation: " + commandLine);

                    response = _tupleSpaceAPI.take(tuple);
                    Console.Write("Response: ");
                    if (response.Size == 0) {
                        Console.WriteLine("No match found\n");
                    }
                    else {
                        Console.WriteLine(response.ToString() + "\n");
                    }
                    break;

                case "wait":
                    System.Threading.Thread.Sleep(int.Parse(commandItems[1]));
                    Console.WriteLine("Operation: " + commandLine + "\n");
                    break;
            }
        }

        public void executeScript(string scriptName)
        {


            var watch = System.Diagnostics.Stopwatch.StartNew();




            StreamReader reader;
            try {
                reader = File.OpenText("../../../Client/bin/debug/" + scriptName);
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



            watch.Stop();
            var elapsedTime = watch.ElapsedMilliseconds;
            Console.WriteLine("Elapsed time: " + elapsedTime);



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
