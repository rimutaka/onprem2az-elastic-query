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
            public GenericConfigEntry Merge(GenericConfigEntry mergeFrom)
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
                    if (to == null) myType.GetField(name).SetValue(this, from);
                }

                return this;
            }

            /// <summary>
            /// Save a single config file. No overwrites.
            /// </summary>
            public static void SaveConfigFile(string configType, string destFolder, string configContents)
            {
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

            /// <summary>
            /// Save a single config file. No overwrites.
            /// </summary>
            /// <param name="config"></param>
            /// <param name="destFolder"></param>
            /// <param name="configContents"></param>
            public static void SaveConfigFile(Configs.GenericConfigEntry config, string destFolder, string configContents)
            {
                string configType = config.GetType().Name;
                SaveConfigFile(configType, destFolder, configContents);
            }

        }

        public class CreateMasterKey : GenericConfigEntry
        {
            public string folder;
            public string localDB;
            public string password;
            public string credential;
            public string identity;
            public string secret;
        }

        public class CreateExternalDataSource : GenericConfigEntry
        {
            public string folder;
            public string localDB;
            public string externalDB;
            public string serverName;
            public string credential;
            public string twoway;
        }

        public class CreateMasterMirror : GenericConfigEntry
        {
            public string folder;
            public string masterDB;
            public string mirrorDB;
            public string table;
        }

        public class CreateTable : GenericConfigEntry
        {
            public string folder;
            public string localDB;
            public string remoteDB;
            public string remoteCS;
            public string table;
        }

        public class GenerateTableList : GenericConfigEntry
        {
            public string folder;
            public string masterTables;
            public string mirrorDB;
            public string serverName;
            public string password;
            public string credential;
            public string identity;
            public string secret;
            public string twoway;
            public string connections;
        }

    }
}
