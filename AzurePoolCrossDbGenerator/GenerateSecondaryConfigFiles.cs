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
        public static void GenerateSecondaryConfigFiles(Configs.InitialConfig config)
        {
            // output collections per config file
            List<Configs.AllTables> tableListMirror = new List<Configs.AllTables>(); // config for mirror tables
            List<Configs.AllTables> tableListRO = new List<Configs.AllTables>(); // config for read-only external tables
            List<Configs.CreateMasterKey> masterKeyList = new List<Configs.CreateMasterKey>(); // config for Master Key config
            List<Configs.CreateExternalDataSource> extDataSrcList = new List<Configs.CreateExternalDataSource>(); // config for ext data source config

            Configs.AllTables prevTable = new Configs.AllTables(); // a container for tracking changes

            // normalise line endings and remove [ ]
            config.masterTables ??= "";
            config.masterTablesRO ??= "";
            config.masterTables = config.masterTables.Replace("\r", "").Replace("[", "").Replace("]", "").Replace(" ", "");
            config.masterTablesRO = config.masterTablesRO.Replace("\r", "").Replace("[", "").Replace("]", "").Replace(" ", "");
            config.connections = config.connections.Replace("\r", "");

            var jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }; // ignore null properties

            // combine mirror and ro lists
            string[] allTables = config.masterTables.Split("\n");
            string[] roTables = config.masterTablesRO.Split("\n");
            int mirrorCount = allTables.Length;
            if (roTables.Length > 0)
            {
                Array.Resize<string>(ref allTables, mirrorCount + roTables.Length);
                Array.Copy(roTables, 0, allTables, mirrorCount, roTables.Length);
            }

            foreach (string tableLine in allTables)
            {
                mirrorCount--; // decrement the counter to know when mirrors turn into read-only tables
                if (mirrorCount == -1) prevTable = new Configs.AllTables(); // reset on switching from mirrors to ROs

                // get the 3-part name like DB_STATS..TB_MANUALRESERVATION
                if (string.IsNullOrWhiteSpace(tableLine)) continue; // skip empty lines
                string[] tableParts = tableLine.Trim().Split(".");

                // check the format
                if (tableParts.Length != 3) throw new Exception($"Must be a 3-part name: {tableLine}");
                if (tableParts[0].ToLower()==config.mirrorDB.ToLower()) throw new Exception($"Found a self-reference: {tableLine}");

                // add mirror table details
                var tableItem = new Configs.AllTables()
                {
                    masterTable = tableParts[2],
                    masterDB = (prevTable.masterDB?.ToLower() != tableParts[0].ToLower()) ? tableParts[0] : null,
                    mirrorDB = (prevTable.mirrorDB?.ToLower() != config.mirrorDB.ToLower()) ? config.mirrorDB : null,
                    masterCS = (prevTable.masterDB?.ToLower() != tableParts[0].ToLower()) ? GetConnectionString(config, tableParts[0]) : null
                };

                if (mirrorCount >= 0)
                {
                    tableListMirror.Add(tableItem); // add to mirror collection
                }
                else
                {
                    tableListRO.Add(tableItem); // add to read-only collection
                }

                prevTable.Merge(tableItem, true); // merge with overwrite

                // process MasterKeyConfig
                var masterKeyItem = new Configs.CreateMasterKey();
                if (masterKeyList.Count == 0) // add full details to the first item only
                {
                    masterKeyItem.password = config.password;
                    masterKeyItem.credential = config.credential;
                    masterKeyItem.identity = config.identity;
                    masterKeyItem.secret = config.secret;
                    // the very first record is actually for the mirror DB, so we have to re-initialise
                    // not to miss the first master table from the loop
                    masterKeyItem.localDB = config.mirrorDB;
                    masterKeyList.Add(masterKeyItem);
                    masterKeyItem = new Configs.CreateMasterKey();
                }
                if (!string.IsNullOrEmpty(tableItem.masterDB))
                {
                    masterKeyItem.localDB = tableItem.masterDB; // only local db can be added automatically
                    masterKeyList.Add(masterKeyItem);
                }

                // process ExternalDataSource config
                var extDataSrcItem = new Configs.CreateExternalDataSource();
                if (extDataSrcList.Count == 0) // add full details to the first item only
                {
                    extDataSrcItem.serverName = config.serverName;
                    extDataSrcItem.credential = config.credential;
                    extDataSrcItem.twoway = config.twoway;
                    extDataSrcItem.externalDB = config.mirrorDB;

                }
                if (!string.IsNullOrEmpty(tableItem.masterDB))
                {
                    extDataSrcItem.localDB = tableItem.masterDB; // only local db can be added automatically
                    extDataSrcList.Add(extDataSrcItem);
                }

                // check if the table exists in Master DB
                string tableCols = DbAccess.GetTableColumns(prevTable.masterCS, prevTable.masterTable);
                if (string.IsNullOrEmpty(tableCols))
                {
                    Console.WriteLine();
                    Console.WriteLine($"Missing table definition for {prevTable.masterDB}..{prevTable.masterTable}");
                    Program.ExitApp();
                }
            }

            // save as files
            if (tableListMirror.Count > 0) Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.TablesConfigMirror, JsonConvert.SerializeObject(tableListMirror.ToArray(), jsonSettings));
            if (tableListRO.Count > 0) Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.TablesConfigReadOnly, JsonConvert.SerializeObject(tableListRO.ToArray(), jsonSettings));
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.MasterKeyConfig, JsonConvert.SerializeObject(masterKeyList.ToArray(), jsonSettings));
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.ExternalDataSourceConfig, JsonConvert.SerializeObject(extDataSrcList.ToArray(), jsonSettings));
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
                Console.WriteLine($"{config.mirrorDB} / {dbName}: missing connection string.");
                Program.ExitApp();
            }

            return cs;
        }

    }
}
