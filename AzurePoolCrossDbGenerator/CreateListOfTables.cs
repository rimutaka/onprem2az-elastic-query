using System;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Genrate a generic list of tables as JSON for further editing. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void GenerateListOfTables(string configJson, string templateFolder)
        {
            // load data
            Configs.GenerateTableList config = JsonConvert.DeserializeObject<Configs.GenerateTableList>(configJson);

            List<Configs.CreateTable> mirrorTableList = new List<Configs.CreateTable>(); // config for mirror tables
            List<Configs.CreateTable> masterExtTableList = new List<Configs.CreateTable>(); // config for master ext tables

            // normalise line endings
            config.masterTables = config.masterTables.Replace("\r", "");
            config.connections = config.connections.Replace("\r", "");

            string prevMasterDbName = ""; // used to track DB name changes
            var jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }; // ignore null properties

            // extract the connection string from master to mirror
            string csToMirror = GetConnectionString(config, config.mirrorDB);

            foreach (string tableLine in config.masterTables.Split("\n"))
            {
                // get the 3-part name like DB_STATS..TB_MANUALRESERVATION
                if (string.IsNullOrWhiteSpace(tableLine)) continue; // skip empty lines
                string[] tableParts = tableLine.Trim().Split(".");

                // check the format
                if (tableParts.Length != 3) throw new Exception($"Must be a 3-part name: {tableLine}");

                // extract the connection string to master from mirror
                string csToMaster = null;
                if (prevMasterDbName != tableParts[0].ToLower())
                {
                    string regexPattern = $".*Initial Catalog={tableParts[0]};.*";
                    csToMaster = GetConnectionString(config, tableParts[0]);
                    if (string.IsNullOrEmpty(csToMaster))
                    {
                        csToMaster = "connection_string_required"; // a placeholder in case the CS is missing
                        Console.WriteLine($"{config.mirrorDB}: missing connection string.");
                    }
                    prevMasterDbName = tableParts[0].ToLower(); // store the current value
                }

                // add mirror table details
                var mirrorItem = new Configs.CreateTable() { table = tableParts[2], remoteCS = csToMaster };
                mirrorItem.remoteDB = (csToMaster == null) ? null : tableParts[0]; // only add DB name if it changed from the previous entry
                if (mirrorTableList.Count == 0)
                {
                    // initial settings for all mirror items
                    mirrorItem.folder = config.folder;
                    mirrorItem.localDB = config.mirrorDB;
                }
                mirrorTableList.Add(mirrorItem);

                // add master ext table details
                var extItem = new Configs.CreateTable() { localDB = tableParts[0], table = tableParts[2] };
                if (masterExtTableList.Count == 0)
                {
                    // initial settings for all master items
                    extItem.folder = config.folder;
                    extItem.remoteDB = config.mirrorDB;
                    extItem.remoteCS = csToMirror;
                }
                masterExtTableList.Add(extItem);
            }

            // convert to arrays for serializing
            Configs.CreateTable[] mirrorTableListArray = mirrorTableList.ToArray();
            Configs.CreateTable[] extTableListArray = masterExtTableList.ToArray();

            // save as files
            Configs.GenericConfigEntry.SaveConfigFile("CreateMirrorTables", config.folder, JsonConvert.SerializeObject(mirrorTableListArray, jsonSettings));
            Configs.GenericConfigEntry.SaveConfigFile("CreateExternalTables", config.folder, JsonConvert.SerializeObject(extTableListArray, jsonSettings));

        }

        /// <summary>
        /// Get a connection string for the matching DB or log an error.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        static string GetConnectionString(Configs.GenerateTableList config, string dbName)
        {
            string regexPattern = $".*Initial Catalog={dbName};.*";
            var regexOptions = RegexOptions.Multiline | RegexOptions.IgnoreCase;
            string cs = Regex.Match(config.connections, regexPattern, regexOptions)?.Value;
            if (string.IsNullOrEmpty(cs))
            {
                cs = "connection_string_required"; // a placeholder in case the CS is missing
                Console.WriteLine($"{config.mirrorDB}: missing connection string.");
            }

            return cs;
        }

    }
}
