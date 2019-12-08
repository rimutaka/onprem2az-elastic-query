using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Creates tables and SPs in Master and Mirror DBs. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void CreateMaster(string configJson, string templateFolder)
        {
            // load data
            Configs.CreateMasterMirror[] config = JsonConvert.DeserializeObject<Configs.CreateMasterMirror[]>(configJson);
            Configs.CreateMasterMirror sharedConfig = new Configs.CreateMasterMirror();


            // generate output using different templates
            CreateMaster_Loop("MasterAlterTable", templateFolder, sharedConfig, config);
            CreateMaster_Loop("MasterCreateSP", templateFolder, sharedConfig, config);
            //CreateMaster_Loop("MirrorAlterTable", templateFolder, sharedConfig, config);
            //CreateMaster_Loop("MirrorSP", templateFolder, sharedConfig, config);


        }

        /// <summary>
        /// A repeatable part of CreateMaster for different templates
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="templateFolder"></param>
        /// <param name="sharedConfig"></param>
        /// <param name="config"></param>
        static void CreateMaster_Loop(string templateName, string templateFolder, Configs.CreateMasterMirror sharedConfig, Configs.CreateMasterMirror[] config)
        {
            string templateContents = Generators.GetTemplateContents(templateFolder, templateName+".txt");

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.CreateMasterMirror)config[i].Merge(sharedConfig);
                if (!Generators.IsDestFolderOK(config[i].folder, i)) continue;

                // interpolate
                string outputContents = string.Format(templateContents, config[i].masterDB, config[i].table, config[i].mirrorDB);

                string outputFileName = Path.Combine(config[i].folder, $"{templateName}_{config[i].masterDB}_{config[i].mirrorDB}{fileExtSQL}");

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }
        }

    }
}
