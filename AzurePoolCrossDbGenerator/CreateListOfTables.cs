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
        public static void GenerateListOfTables(string configJson)
        {
            // load data
            Configs.InitialConfig config = JsonConvert.DeserializeObject<Configs.InitialConfig>(configJson);

            List<Configs.AllTables> tableList = new List<Configs.AllTables>(); // config for mirror tables

            Configs.AllTables prevTable = new Configs.AllTables(); // a container for tracking changes

            // normalise line endings and remove [ ]
            config.masterTables = config.masterTables.Replace("\r", "").Replace("[", "").Replace("]", "");
            config.connections = config.connections.Replace("\r", "");

            var jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }; // ignore null properties

            foreach (string tableLine in config.masterTables.Split("\n"))
            {
                // get the 3-part name like DB_STATS..TB_MANUALRESERVATION
                if (string.IsNullOrWhiteSpace(tableLine)) continue; // skip empty lines
                string[] tableParts = tableLine.Trim().Split(".");

                // check the format
                if (tableParts.Length != 3) throw new Exception($"Must be a 3-part name: {tableLine}");

                // add mirror table details
                var tableItem = new Configs.AllTables()
                {
                    masterTable = tableParts[2],
                    masterDB = (prevTable.masterDB?.ToLower() != tableParts[0].ToLower()) ? tableParts[0] : null,
                    mirrorDB = (prevTable.mirrorDB?.ToLower() != config.mirrorDB.ToLower()) ? config.mirrorDB : null,
                    masterCS = (prevTable.masterDB?.ToLower() != tableParts[0].ToLower()) ? GetConnectionString(config, tableParts[0]) : null
                };

                tableList.Add(tableItem); // add to the collection

                prevTable.Merge(tableItem, true); // merge with overwrite

            }

            // convert to arrays for serializing
            Configs.AllTables[] tableListArray = tableList.ToArray();

            // save as files
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.TablesConfig, JsonConvert.SerializeObject(tableListArray, jsonSettings));

        }

        /// <summary>
        /// Get a connection string for the matching DB or log an error.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        static string GetConnectionString(Configs.InitialConfig config, string dbName)
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
