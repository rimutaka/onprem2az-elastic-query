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
        public static void GenerateBlankConfigs()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string configFolder = Path.Combine(currentDirectory, Program.FileNames.ConfigFolder);

            // create the folder if it doesn't exist
            if (!Directory.Exists(configFolder)) Directory.CreateDirectory(configFolder);

            Console.WriteLine($"Writing config files to {configFolder}");

            // Create Master Key config
            Configs.GenericConfigEntry[] config = { new Configs.CreateMasterKey() };
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.MasterKeyConfig, JsonConvert.SerializeObject(config));

            // Create data sources config
            config[0] = new Configs.CreateExternalDataSource();
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.ExternalDataSourceConfig, JsonConvert.SerializeObject(config));

            // create the initial config file
            config[0] = new Configs.InitialConfig();
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.InitialConfig, JsonConvert.SerializeObject(config[0]));

            // create search-n-replace config file
            config[0] = new Configs.SearchAndReplace();
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.SearchAndReplaceConfig, JsonConvert.SerializeObject(config[0]));

            // copy all templates to the local templates folder
            string templatesFolderDest = Path.Combine(currentDirectory, Program.FileNames.TemplatesFolder);
            string templatesFolderSrc = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Program.FileNames.TemplatesFolder);

            if (!Directory.Exists(templatesFolderDest)) Directory.CreateDirectory(templatesFolderDest);
            Console.WriteLine($"Writing template files to {templatesFolderDest}");



            // Copy the files and overwrite destination files if they already exist.
            foreach (string templateFile in Directory.GetFiles(templatesFolderSrc))
            {
                string templateFileNoPath = Path.GetFileName(templateFile);
                string fileNameDest = Path.Combine(templatesFolderDest, templateFileNoPath);
                if (File.Exists(fileNameDest))
                {
                    Console.WriteLine($"{templateFileNoPath} already exists.");
                }
                else
                {
                    File.Copy(templateFile, fileNameDest, false);
                    Console.WriteLine($"{templateFileNoPath} written.");
                }
            }
        }
    }
}
