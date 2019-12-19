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
        public static void GenerateScript(Configs.AllTables[] config, string templateFileName, string paramRunOn)

        {
            // load config data
            Configs.AllTables sharedConfig = new Configs.AllTables();

            string templateContents = Generators.GetTemplateContents(templateFileName);

            string paramFileNameTemplate = null;

            switch (paramRunOn)
            {
                case "master":
                    {
                        paramFileNameTemplate = Program.FileNames.OutputFileNameMaskRunOnMaster;
                        break;
                    }
                case "mirror":
                    {
                        paramFileNameTemplate = Program.FileNames.OutputFileNameMaskRunOnMirror;
                        break;
                    }
                default: 
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Missing parameter -o [master | mirror] to tell SqlCmd which db to run the script on.");
                        Program.ExitApp();
                        break;
                    }
            }

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.AllTables)config[i].Merge(sharedConfig);

                // get the column list if there is {3} group in the template
                string tableCols = null;
                if (templateContents.Contains("{3}"))
                {
                    tableCols = DbAccess.GetTableColumns(config[i].masterCS, config[i].masterTable);

                    if (string.IsNullOrEmpty(tableCols))
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Missing table definition for {config[i].masterDB}..{config[i].masterTable}");
                        Program.ExitApp();
                    }

                }

                // interpolate
                string outputContents = string.Format(templateContents, config[i].mirrorDB, config[i].masterDB, config[i].masterTable, tableCols);

                string fileSuffix = string.Format(paramFileNameTemplate, config[i].mirrorDB, config[i].masterDB, config[i].masterTable);

                string outputFileName = $"{Path.GetFileNameWithoutExtension(templateFileName)}__{fileSuffix}{fileExtSQL}";

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }

        }

    }
}
