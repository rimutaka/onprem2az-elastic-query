using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Use an arbitrary template to generate an SQL script. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void GenerateScript(string configJson, string templateFileName)
        {

            // load config data
            Configs.AllTables[] config = JsonConvert.DeserializeObject<Configs.AllTables[]>(configJson);
            Configs.AllTables sharedConfig = new Configs.AllTables();

            string templateContents = Generators.GetTemplateContents(templateFileName);

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.AllTables)config[i].Merge(sharedConfig);

                // get the column list if there is {3} group in the template
                string tableCols =(templateContents.Contains("{3}")) ? DbAccess.GetTableColumns(config[i].masterCS, config[i].masterTable) : null;

                // interpolate
                string outputContents = string.Format(templateContents, config[i].mirrorDB, config[i].masterDB, config[i].masterTable, tableCols);

                string outputFileName = $"{Path.GetFileNameWithoutExtension(templateFileName)}_{config[i].masterDB}__{config[i].mirrorDB}__{config[i].masterTable}{fileExtSQL}";

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }

        }

    }
}
