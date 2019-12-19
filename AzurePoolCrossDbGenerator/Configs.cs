using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AzurePoolCrossDbGenerator
{
    public class Configs
    {
        /// <summary>
        /// A base class for multiple configuration types.
        /// </summary>
        public abstract class GenericConfigEntry
        {


            /// <summary>
            /// Add missing values from mergeFrom to this.
            /// </summary>
            /// <param name="mergeFrom"></param>
            public GenericConfigEntry Merge(GenericConfigEntry mergeFrom, bool overwrite = false)
            {
                // get list of public fields
                Type myType = mergeFrom.GetType();
                FieldInfo[] myField = myType.GetFields();

                // copy values on those with missing values
                foreach (var field in myField)
                {
                    string name = field.Name;
                    string from = (string)myType.GetField(name).GetValue(mergeFrom);
                    string to = (string)myType.GetField(name).GetValue(this);
                    if (to == null || (overwrite && from != null)) myType.GetField(name).SetValue(this, from);
                }

                return this;
            }

            /// <summary>
            /// Save a single config file. No overwrites.
            /// </summary>
            public static void SaveConfigFile(string FileName, string configContents)
            {
                string currentDirectory = Directory.GetCurrentDirectory();

                string configPath = Path.Combine(currentDirectory, FileName);
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"{FileName} already exists.");
                }
                else
                {
                    File.WriteAllText(configPath, configContents, System.Text.Encoding.UTF8);
                    Console.WriteLine($"{FileName} written.");
                }
            }
        }

        public class CreateMasterKey : GenericConfigEntry
        {
            public string localDB;
            public string password;
            public string credential;
            public string identity;
            public string secret;

            public static CreateMasterKey[] Load(string configFileName)
            {
                configFileName ??= Program.FileNames.MasterKeyConfig;
                return JsonConvert.DeserializeObject<Configs.CreateMasterKey[]>(LoadConfigFile(configFileName));
            }

        }

        public class CreateExternalDataSource : GenericConfigEntry
        {
            public string localDB;
            public string externalDB;
            public string serverName;
            public string credential;
            public string twoway;

            public static CreateExternalDataSource[] Load(string configFileName)
            {
                configFileName ??= Program.FileNames.ExternalDataSourceConfig;
                return JsonConvert.DeserializeObject<Configs.CreateExternalDataSource[]>(LoadConfigFile(configFileName));
            }
        }

        public class AllTables : GenericConfigEntry
        {
            public string mirrorDB;
            public string masterDB;
            public string masterCS;
            public string masterTable;

            public static AllTables[] Load(string configFileName)
            {
                return JsonConvert.DeserializeObject<Configs.AllTables[]>(LoadConfigFile(configFileName));
            }
        }

        public class InitialConfig : GenericConfigEntry
        {
            public string masterTables;
            public string masterTablesRO;
            public string mirrorDB;
            public string serverName;
            public string password;
            public string credential;
            public string identity;
            public string secret;
            public string twoway;
            public string connections;
            public string localServer;

            public static InitialConfig Load(string configFileName)
            {
                configFileName ??= Program.FileNames.InitialConfig;
                return JsonConvert.DeserializeObject<Configs.InitialConfig>(LoadConfigFile(configFileName));
            }
        }

        /// <summary>
        /// Load the config file or notify of problems and exit.
        /// </summary>
        /// <param name="configFileName">Can be rooted or relative to the current directory.</param>
        /// <returns></returns>
        static string LoadConfigFile(string configFileName)
        {

            if (string.IsNullOrEmpty(configFileName))
            {
                Console.WriteLine();
                Console.WriteLine($"Use -c full_path or -c relative_path.");
                Program.ExitApp();
            }

            // build the full path depending on the input
            if (!configFileName.Contains("\\") && !configFileName.Contains("/")) configFileName = Path.Combine(Program.FileNames.ConfigFolder, configFileName);
            string fullPath = (Path.IsPathRooted(configFileName)) ? configFileName : Path.Combine(Directory.GetCurrentDirectory(), configFileName);

            // check if the file exists
            if (!System.IO.File.Exists(fullPath))
            {
                Console.WriteLine();
                Console.WriteLine($"Missing config file: {fullPath}. Use -c full_path or -c relative_path or -c file_name_in_scripts_subfolder.");
                Program.ExitApp();
            }

            // load the config file
            string configJson = null;
            try
            {
                configJson = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("Cannot read the config file: " + ex.Message);
                Program.ExitApp();
            }

            return configJson;
        }
    }
}
