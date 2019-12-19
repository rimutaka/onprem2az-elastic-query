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
        public static void CreateExternalDataSource(Configs.CreateExternalDataSource[] config)
        {
            // load data
            Configs.CreateExternalDataSource sharedConfig = new Configs.CreateExternalDataSource();
            string templateContents = Generators.GetTemplateContents("CreateExternalDataSource.txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateExternalDataSource)config[i].Merge(sharedConfig);

                // there may be 2 loops if it generates scripts for mutual access
                for (int j = 0; j < 2; j++)
                {
                    // interpolate
                    string outputContents = string.Format(templateContents,
                        config[i].localDB, config[i].externalDB,
                        config[i].serverName, config[i].credential);

                    string outputFileName = $"CreateExternalDataSource_{config[i].localDB}__{config[i].externalDB}{fileExtSQL}";

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
