﻿using System;
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
        }

        public class CreateExternalDataSource : GenericConfigEntry
        {
            public string localDB;
            public string externalDB;
            public string serverName;
            public string credential;
            public string twoway;
        }

        public class AllTables : GenericConfigEntry
        {
            public string mirrorDB;
            public string masterDB;
            public string masterCS;
            public string masterTable;
        }

        public class InitialConfig : GenericConfigEntry
        {
            public string masterTables;
            public string mirrorDB;
            public string serverName;
            public string password;
            public string credential;
            public string identity;
            public string secret;
            public string twoway;
            public string connections;
            public string localServer;
        }

        public class SearchAndReplace : GenericConfigEntry
        {
            public string localServer;
            public string patternSelfRefs = "{1}.{2}";
            public string nameMirror = "mr_{0}__{2}";
            public string nameExtTable = "ext_{0}__{2}";

        }
    }
}
