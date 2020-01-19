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
        public static void CreateMasterKey(Configs.CreateMasterKey[] config)
        {
            // load data
            Configs.CreateMasterKey sharedConfig = new Configs.CreateMasterKey();
            string templateContents = Generators.GetTemplateContents("CreateMasterKey.txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateMasterKey)config[i].Merge(sharedConfig);

                // interpolate
                string outputContents = string.Format(templateContents, config[i].localDB, config[i].password, config[i].credential, config[i].identity, config[i].secret);

                string outputFileName = $"CreateMasterKey__{config[i].localDB}__x__x{fileExtSQL}";

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }
        }

    }
}
