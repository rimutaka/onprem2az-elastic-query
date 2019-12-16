using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    class Program
    {

        public static readonly string templateFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"Templates");

        static void Main(string[] args)
        {
            Console.WriteLine("AzurePoolCrossDbGenerator started.");

            string command = (args.Length > 0) ? args[0]?.Trim().ToLower() : ""; // must be a valid command
            string scriptTemplateFileName = (args.Length > 1) ? args[1] : ""; // a file name with the template to use

            // validate the params
            if (string.IsNullOrEmpty(command))
            {
                Console.WriteLine($"1. Check `readme.md` for usage instructions.");
                Console.WriteLine($"2. Use `{Commands.GenerateBlankConfigFiles}` command to initialise the environment.");
                Console.WriteLine($"3. Populate `{FileNames.InitialConfig}`, `{FileNames.MasterKeyConfig}` and `{FileNames.ExternalDataSourceConfig}` files.");
                Console.WriteLine($"4. Use `{Commands.GenerateTablesConfigFile}` command to build the list of tables.");
                Console.WriteLine($"5. Check if `{FileNames.TablesConfig}` file is correct.");
                Console.WriteLine($"6. Use other commands to generate scripts.");
                ExitApp();
            }

            // check there are 2 args for generic script generation
            if (command == Commands.GenericScriptGeneration && string.IsNullOrEmpty(scriptTemplateFileName))
            {
                Console.WriteLine("Required format: generate [path to the template file]. See readme.md for usage instructions.");
                return;
            }

            // call the handler for the command
            switch (command)
            {
                case Commands.GenerateMasterKeys:
                    {
                        Generators.CreateMasterKey(LoadConfigFile(FileNames.MasterKeyConfig));
                        break;
                    }
                case Commands.GenerateExternalDataSources:
                    {
                        Generators.CreateExternalDataSource(LoadConfigFile(FileNames.ExternalDataSourceConfig));
                        break;
                    }
                case Commands.GenerateBlankConfigFiles:
                    {
                        Generators.GenerateBlankConfigs();
                        break;
                    }
                case Commands.GenerateTablesConfigFile:
                    {
                        Generators.GenerateListOfTables(LoadConfigFile(FileNames.InitialConfig));
                        break;
                    }
                case Commands.GenericScriptGeneration:
                    {
                        Generators.GenerateScript(LoadConfigFile(FileNames.TablesConfig), scriptTemplateFileName);
                        break;
                    }
                case Commands.GenerateSqlCmdBatch:
                    {
                        Generators.GenerateSqlCmdBatch(LoadConfigFile(FileNames.InitialConfig), scriptTemplateFileName);
                        break;
                    }
                case Commands.RemoveSelfReferences:
                    {
                        Generators.SearchAndReplace(LoadConfigFile(FileNames.SearchAndReplaceConfig), scriptTemplateFileName, Generators.GetNewObjectNameSelfRefs);
                        break;
                    }
                case Commands.RemoveInsertReferences:
                    {
                        Generators.SearchAndReplace(LoadConfigFile(FileNames.SearchAndReplaceConfig), scriptTemplateFileName, Generators.GetNewObjectNameInsert);
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Wrong command. See readme.md for usage instructions.");
                        break;
                    }
            }

            ExitApp(0);
        }

        /// <summary>
        /// Load the config file or notify of problems and exit.
        /// </summary>
        /// <param name="configFileName"></param>
        /// <returns></returns>
        static string LoadConfigFile(string configFileName)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), configFileName);

            // check if the file exists
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine($"Missing config file: {fullPath}. Use `config` command to initialise the environment.");
                ExitApp();
            }

            // load the config file
            string configJson = null;
            try
            {
                configJson = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot read the config file: " + ex.Message);
                ExitApp();
            }

            return configJson;
        }




        /// <summary>
        /// Wait for any key before exiting.
        /// </summary>
        public static void ExitApp(int exitCode = 1)
        {
            Console.WriteLine("Done. Exiting the app.");
            //Console.ReadKey();
            Environment.Exit(exitCode);
        }


        public class FileNames
        {
            public const string ConfigFolder = "config";
            public const string TemplatesFolder = "templates";
            public const string OutputFolder = "scripts";
            public const string InitialConfig = "config/config.json";
            public const string ExternalDataSourceConfig = "config/ExternalDataSourceConfig.json";
            public const string TablesConfig = "config/TablesConfig.json";
            public const string MasterKeyConfig = "config/MasterKeyConfig.json";
            public const string SearchAndReplaceConfig = "config/SearchAndReplaceConfig.json";
        }

        public class Commands
        {
            public const string GenerateMasterKeys = "keys";
            public const string GenerateExternalDataSources = "sources";
            public const string GenerateBlankConfigFiles = "init";
            public const string GenerateTablesConfigFile = "config";
            public const string GenericScriptGeneration = "template";
            public const string GenerateSqlCmdBatch = "sqlcmd";
            public const string RemoveSelfReferences = "selfref";
            public const string RemoveInsertReferences = "insertref";
        }

    }
}
