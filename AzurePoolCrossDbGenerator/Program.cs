using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    class Program
    {
        // List of known commands
        public const string commandKey = "key", commandSource = "source", commandConfig = "config";
        public static readonly string templateFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"Templates");

        static void Main(string[] args)
        {
            Console.WriteLine("AzurePoolCrossDbGenerator started.");



            string command = (args.Length > 0) ? args[0]?.Trim().ToLower():""; // must be a valid command
            string configFileName =(args.Length>1)?args[1]:""; // full path to the config file to process

            // check there are 2 args
            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(configFileName))
            {
                Console.WriteLine("Required format: [command] [path]. See readme.md for usage instructions.");
                return;
            }

            
            // config command doesn't need a config file - process it first
            if (command == commandConfig)
            {
               Generators.GenerateBlankConfigs(configFileName);
                PreExit();
                return;
            }

            // all other commands require a config file - check that it exists first
            if (!System.IO.File.Exists(configFileName))
            {
                Console.WriteLine("2nd param is invalid. Must be a valid config file name.");
                PreExit();
                return;
            }

            // load the config file
            string configJson=null;
            if (!string.IsNullOrEmpty(configFileName))
            {
                try
                {
                    configJson = File.ReadAllText(configFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot read the config file: " + ex.Message);
                    PreExit();
                    return;
                }
            }

            // call the handler for the command
            switch (command)
            {
                case commandKey:
                    {
                        Generators.CreateMasterKey(configJson, templateFolder);
                        break;
                    }
                case commandSource:
                    {
                        Generators.CreateExternalDataSource(configJson, templateFolder);
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Wrong command. See readme.md for usage instructions.");
                        break;
                    }
            }

            PreExit();
        }

        /// <summary>
        /// Wait for any key before exiting.
        /// </summary>
        static void PreExit ()
        {
            Console.WriteLine("Done. Press any key to exit");
            Console.ReadKey();
        }

 

  

    }
}
