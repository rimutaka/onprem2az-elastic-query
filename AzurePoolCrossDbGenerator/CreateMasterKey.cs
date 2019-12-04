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
        public static void CreateMasterKey(string configJson, string templateFolder)
        {
            // load data
            Configs.CreateMasterKey[] config = JsonConvert.DeserializeObject<Configs.CreateMasterKey[]>(configJson);
            Configs.CreateMasterKey sharedConfig = new Configs.CreateMasterKey();
            string templateContents = Generators.GetTemplateContents(templateFolder, "CreateMasterKey.txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateMasterKey)config[i].Merge(sharedConfig);
                if (!Generators.IsDestFolderOK(config[i].folder, i)) continue;

                // interpolate
                string outputContents = string.Format(templateContents, config[i].localDB, config[i].password, config[i].credential, config[i].identity, config[i].secret);

                string outputFileName = Path.Combine(config[i].folder, $"CreateMasterKey_{config[i].localDB}{fileExtSQL}");

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }
        }

    }
}
