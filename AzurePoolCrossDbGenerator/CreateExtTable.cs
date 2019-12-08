using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Genrate scripts for external tables matching the remote definition. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void CreateExtTable(string configJson, string templateFolder)
        {
            // load data
            Configs.CreateTable[] config = JsonConvert.DeserializeObject<Configs.CreateTable[]>(configJson);
            Configs.CreateTable sharedConfig = new Configs.CreateTable();


            string templateContents = Generators.GetTemplateContents(templateFolder, "CreateExtTable.txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateTable)config[i].Merge(sharedConfig);
                if (!Generators.IsDestFolderOK(config[i].folder, i)) continue;

                // get the column list
                string tableCols = DbAccess.GetTableColumns(config[i].remoteCS, config[i].table);

                // interpolate
                string outputContents = string.Format(templateContents, config[i].localDB, config[i].table, config[i].remoteDB, tableCols);

                string outputFileName = Path.Combine(config[i].folder, $"CreateExtTable_{config[i].localDB}_{config[i].remoteDB}_{config[i].table}{fileExtSQL}");

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }

        }

    }
}
