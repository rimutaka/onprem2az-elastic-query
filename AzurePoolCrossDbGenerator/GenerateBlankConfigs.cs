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

            Program.WriteLine($"Writing config files to {configFolder}");

            // create the initial config file
            var config = new Configs.InitialConfig();
            Configs.GenericConfigEntry.SaveConfigFile(Program.FileNames.InitialConfig, JsonConvert.SerializeObject(config));

            // copy all templates to the local templates folder
            string templatesFolderDest = Path.Combine(currentDirectory, Program.FileNames.TemplatesFolder);
            string templatesFolderSrc = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), Program.FileNames.TemplatesFolder);

            if (!Directory.Exists(templatesFolderDest)) Directory.CreateDirectory(templatesFolderDest);
            Program.WriteLine($"Writing template files to {templatesFolderDest}");



            // Copy the files and overwrite destination files if they already exist.
            foreach (string templateFile in Directory.GetFiles(templatesFolderSrc))
            {
                string templateFileNoPath = Path.GetFileName(templateFile);
                string fileNameDest = Path.Combine(templatesFolderDest, templateFileNoPath);
                if (File.Exists(fileNameDest))
                {
                    Program.WriteLine($"{templateFileNoPath} already exists.", ConsoleColor.Yellow);
                }
                else
                {
                    File.Copy(templateFile, fileNameDest, false);
                    Program.WriteLine($"{templateFileNoPath} written.");
                }
            }
        }
    }
}
