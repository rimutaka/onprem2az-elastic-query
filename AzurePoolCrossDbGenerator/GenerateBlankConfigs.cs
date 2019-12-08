using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Write out blank config files for all known structures in class Configs.
        /// </summary>
        /// <param name="destFolder"></param>
        public static void GenerateBlankConfigs(string destFolder)
        {
            // check the folder exists
            if (!Directory.Exists(destFolder))
            {
                Console.WriteLine($"Invalid destination folder: {destFolder}");
                return;
            }

            Console.WriteLine($"Writing files to {destFolder}");

            // CreateMasterKey
            Configs.GenericConfigEntry[] config = { new Configs.CreateMasterKey() };
            Configs.GenericConfigEntry.SaveConfigFile(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.CreateExternalDataSource();
            Configs.GenericConfigEntry.SaveConfigFile(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.CreateMasterMirror();
            Configs.GenericConfigEntry.SaveConfigFile(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.CreateTable();
            Configs.GenericConfigEntry.SaveConfigFile(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.GenerateTableList();
            Configs.GenericConfigEntry.SaveConfigFile(config[0], destFolder, JsonConvert.SerializeObject(config));
        }





    }
}
