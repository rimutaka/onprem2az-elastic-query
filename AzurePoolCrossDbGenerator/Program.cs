using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    class Program
    {
        public const string fileExtSQL = ".sql";

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
                GenerateBlankConfigs(configFileName);
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
                        CreateMasterKey(configJson);
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

        /// <summary>
        /// Generates *CREATE MASTER KEY* statements 
        /// </summary>
        /// <param name="configFile"></param>
        static void CreateMasterKey(string configJson)
        {
            // load config
            Configs.CreateMasterKey[] config = JsonConvert.DeserializeObject<Configs.CreateMasterKey[]>(configJson);

            // load the SQL template
            string templatePath = Path.Combine(templateFolder, "CreateMasterKey.txt");
            string templateContents = File.ReadAllText(templatePath);

            Configs.CreateMasterKey sharedConfig = new Configs.CreateMasterKey();

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                config[i].Merge(sharedConfig);
                sharedConfig = config[i];

                // check if the destination file was specified
                if (string.IsNullOrEmpty(config[i].folder))
                {
                    Console.WriteLine($"#{i.ToString()} - no destination folder");
                    continue;
                }

                // interpolate
                string outputContents = string.Format(templateContents, config[i].localDB, config[i].password, config[i].credential, config[i].identity, config[i].secret);

                string outputFileName = Path.Combine(config[i].folder, $"CreateMasterKey_{config[i].localDB}{fileExtSQL}");

                Console.WriteLine($"#{(i+1).ToString()} - saving to {outputFileName}");

                try
                {
                    File.WriteAllText(outputFileName, outputContents, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// Write out blank config files for all known structures in class Configs.
        /// </summary>
        /// <param name="destFolder"></param>
        static void GenerateBlankConfigs(string destFolder)
        {
            // check the folder exists
            if(!Directory.Exists(destFolder))
            {
                Console.WriteLine($"Invalid destination folder: {destFolder}");
                return;
            }

            Console.WriteLine($"Writing files to {destFolder}");

            // CreateMasterKey
            Configs.GenericConfigEntry[] config = { new Configs.CreateMasterKey() };
            SaveBlankConfig(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.CreateExternalDataSource();
            SaveBlankConfig(config[0], destFolder, JsonConvert.SerializeObject(config));
        }

        /// <summary>
        /// Save a single config file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="destFolder"></param>
        /// <param name="configContents"></param>
        static void SaveBlankConfig(Configs.GenericConfigEntry config, string destFolder, string configContents)
        {
            string configType = config.GetType().Name;
            string configPath = Path.Combine(destFolder, $"{configType}.json");
            if (File.Exists(configPath))
            {
                Console.WriteLine($"{configType} already exists.");
            }
            else
            {
                File.WriteAllText(configPath, configContents, System.Text.Encoding.UTF8);
                Console.WriteLine($"{configType} written.");
            }
        }


    }
}
