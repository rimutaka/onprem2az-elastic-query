using System;
using Newtonsoft.Json;
using System.IO;
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

            List<Configs.CreateTable> tableList = new List<Configs.CreateTable>();

            // normalise line endings
            config.tables = config.tables.Replace("\r", "");

            foreach (string tableLine in config.tables.Split("\n"))
            {
                // get the 3-part name like DB_STATS..TB_MANUALRESERVATION
                if (string.IsNullOrWhiteSpace(tableLine)) continue; // skip empty lines
                string[] tableParts = tableLine.Trim().Split(".");

                // check the format
                if (tableParts.Length != 3) throw new Exception($"Must be a 3-part name: {tableLine}");

                // add table details
                tableList.Add(new Configs.CreateTable() { remoteDB = tableParts[0], table = tableParts[2] });
            }

            Configs.CreateTable[] tableListArray = tableList.ToArray();

            string configPath = Path.Combine(config.folder, "TableList.json");
            if (File.Exists(configPath))
            {
                Console.WriteLine($"{configPath} already exists.");
            }
            else
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(tableListArray), System.Text.Encoding.UTF8);
                Console.WriteLine($"{configPath} written.");
            }

        }

    }
}
