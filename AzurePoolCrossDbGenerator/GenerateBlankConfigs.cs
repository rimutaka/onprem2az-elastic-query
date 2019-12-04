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
            SaveBlankConfig(config[0], destFolder, JsonConvert.SerializeObject(config));

            config[0] = new Configs.CreateExternalDataSource();
            SaveBlankConfig(config[0], destFolder, JsonConvert.SerializeObject(config));
        }

        /// <summary>
        /// Save a single config file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="destFolder"></param>
        /// <param name="configContents"></param>
        static void SaveBlankConfig(Configs.GenericConfigEntry config, string destFolder, string configContents)
        {
            string configType = config.GetType().Name;
            string configPath = Path.Combine(destFolder, $"{configType}.json");
            if (File.Exists(configPath))
            {
                Console.WriteLine($"{configType} already exists.");
            }
            else
            {
                File.WriteAllText(configPath, configContents, System.Text.Encoding.UTF8);
                Console.WriteLine($"{configType} written.");
            }
        }


    }
}
