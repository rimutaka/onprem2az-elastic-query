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
            Program.WriteLine($"AzurePoolCrossDbGenerator started in {Directory.GetCurrentDirectory()} with params:");

            string command = (args.Length > 0) ? args[0]?.Trim().ToLower() : ""; // must be a valid command
            Program.WriteLine($"Command: {command}");

            string paramTemplate = null, paramConfig = null, paramGrepFileName = null, paramTargetDir = null, paramRunOn = null;

            // extract additional params
            for (int i = 1; i<args.Length-1; i++)
            {
                switch (args[i])
                {
                    case "-t":
                        {
                            paramTemplate = args[i + 1];
                            Program.WriteLine($"Template: {paramTemplate}");
                            break;
                        }
                    case "-c":
                        {
                            paramConfig = args[i + 1];
                            Program.WriteLine($"Config: {paramConfig}");
                            break;
                        }
                    case "-g":
                        {
                            paramGrepFileName = args[i + 1];
                            Program.WriteLine($"Grep: {paramGrepFileName}");
                            break;
                        }
                    case "-d":
                        {
                            paramTargetDir = args[i + 1];
                            Program.WriteLine($"Target dir: {paramTargetDir}");
                            break;
                        }
                    case "-o":
                        {
                            paramRunOn = args[i + 1];
                            Program.WriteLine($"Run on: {paramRunOn}");
                            break;
                        }
                }
            }

            // validate the params
            if (string.IsNullOrEmpty(command)) PrintWelcomeMsg();

            // call the handler for the command
            switch (command)
            {
                case Commands.GenerateBlankConfigFiles:
                    {
                        Generators.GenerateBlankConfigs();
                        break;
                    }
                case Commands.GenerateSecondaryConfigFiles:
                    {
                        Generators.GenerateSecondaryConfigFiles(Configs.InitialConfig.Load(paramConfig));
                        break;
                    }
                case Commands.GenerateMasterKeys:
                    {
                        Generators.CreateMasterKey(Configs.CreateMasterKey.Load(paramConfig));
                        break;
                    }
                case Commands.GenerateExternalDataSources:
                    {
                        Generators.CreateExternalDataSource(Configs.CreateExternalDataSource.Load(paramConfig));
                        break;
                    }
                case Commands.ScriptGenerationForTablesAnsSPs:
                    {
                        Generators.GenerateScript(Configs.AllTables.Load(paramConfig), paramTemplate, paramRunOn);
                        break;
                    }
                case Commands.ScriptGenerationGeneric:
                    {
                        Generators.GenerateScript(Configs.InitialConfig.Load(paramConfig), paramTemplate);
                        break;
                    }
                case Commands.AltTableColumnTypes:
                    {
                        Generators.GenerateAltColumnsScript(Configs.AllTables.Load(paramConfig));
                        break;
                    }
                case Commands.GenerateSqlCmdBatch:
                    {
                        Generators.GenerateSqlCmdBatch(Configs.InitialConfig.Load(paramConfig), paramTargetDir, paramRunOn);
                        break;
                    }
                case Commands.ReplaceInSqlFiles:
                    {
                        Generators.SearchAndReplace(Configs.InitialConfig.Load(paramConfig), paramGrepFileName, paramTemplate);
                        break;
                    }
                default:
                    {
                        PrintWelcomeMsg();
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
                Program.WriteLine();
                Program.WriteLine($"Missing config file: {fullPath}. Use `config` command to initialise the environment.", ConsoleColor.Red);
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
                Program.WriteLine();
                Program.WriteLine("Cannot read the config file: " + ex.Message, ConsoleColor.Red);
                ExitApp();
            }

            return configJson;
        }


        static void PrintWelcomeMsg()
        {
            Program.WriteLine($"Usage: `command` -t `template file name or replacement pattern` -c `config file name`.");
            Program.WriteLine($"No params commands: `init`, `config`.");
            Program.WriteLine($"See `readme.md` for more info.");
            ExitApp();
        }


        /// <summary>
        /// Wait for any key before exiting.
        /// </summary>
        public static void ExitApp(int exitCode = 1)
        {
            Program.WriteLine("Done. Exiting the app.");
            //Console.ReadKey();
            Environment.Exit(exitCode);
        }

        /// <summary>
        /// Write out a console line with a specified colour or the default colour if not specified.
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="consoleColor"></param>
        public static void WriteLine(string Message = "\n", ConsoleColor consoleColor = ConsoleColor.White)
        {
            var colorDefault = Console.ForegroundColor;

            if (consoleColor != ConsoleColor.White) Console.ForegroundColor = consoleColor;

            Console.WriteLine(Message);

            Console.ForegroundColor = colorDefault;
        }

        public class FileNames
        {
            public const string ConfigFolder = "config";
            public const string TemplatesFolder = "templates";
            public const string OutputFolder = "scripts";
            public const string InitialConfig = "config/config.json";
            public const string ExternalDataSourceConfig = "config/ExternalDataSource.json";
            public const string TablesConfigMirror = "config/TablesMirror.json";
            public const string TablesConfigReadOnly = "config/TablesReadOnly.json";
            public const string SPsConfig = "config/ProxySPs.json";
            public const string MasterKeyConfig = "config/MasterKey.json";
            public const string SearchAndReplaceConfig = "config/SearchAndReplace.json";

            public const string OutputFileNameMaskRunOnMaster = "{1}__{0}__{2}";
            public const string OutputFileNameMaskRunOnMirror = "{0}__{1}__{2}";
        }

        public class Commands
        {
            public const string GenerateMasterKeys = "keys";
            public const string GenerateExternalDataSources = "sources";
            public const string GenerateBlankConfigFiles = "init";
            public const string GenerateSecondaryConfigFiles = "config";
            public const string ScriptGenerationForTablesAnsSPs = "template";
            public const string ScriptGenerationGeneric = "interpolate";
            public const string GenerateSqlCmdBatch = "sqlcmd";
            public const string ReplaceInSqlFiles = "replace";
            public const string AltTableColumnTypes = "fixtypes";
        }

    }
}
