using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Generates *CREATE MASTER KEY* statements 
        /// </summary>
        /// <param name="configFile"></param>
        public static void CreateExternalDataSource(string configJson, string templateFolder)
        {
            // load data
            Configs.CreateExternalDataSource[] config = JsonConvert.DeserializeObject<Configs.CreateExternalDataSource[]>(configJson);
            Configs.CreateExternalDataSource sharedConfig = new Configs.CreateExternalDataSource();
            string templateContents = Generators.GetTemplateContents(templateFolder, "CreateExternalDataSource.txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateExternalDataSource)config[i].Merge(sharedConfig);
                if (!Generators.IsDestFolderOK(config[i].folder, i)) continue;

                // there may be 2 loops if it generates scripts for mutual access
                for (int j = 0; j < 2; j++)
                {
                    // interpolate
                    string outputContents = string.Format(templateContents,
                        config[i].localDB, config[i].sourceNamePrefix + config[i].externalDB,
                        config[i].serverName, config[i].credential, config[i].externalDB);

                    string outputFileName = Path.Combine(config[i].folder,
                        $"CreateExtDataSrc_{config[i].localDB}_{config[i].externalDB}{fileExtSQL}");

                    Generators.SaveGeneratedScript(outputContents, outputFileName, i);

                    // exit the loop now if the data source is not reciprocal - one way only
                    if (config[i].twoway != "1" || j == 1) break;

                    // or swap the DBs if it's two-way generation
                    string ldb = config[i].localDB;
                    config[i].localDB = config[i].externalDB;
                    config[i].externalDB = ldb;
                }
            }
        }

    }
}
