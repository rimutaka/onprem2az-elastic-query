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

            string paramFileNameTemplate = GetOutputFileNameMask(paramRunOn);

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.AllTables)config[i].Merge(sharedConfig);

                // get the column list if there is {3} group in the template
                string tableCols = null;
                if (templateContents.Contains("{3}"))
                {
                    tableCols = DbAccess.GetTableColumns(config[i].masterCS, config[i].masterTableOrSP);

                    if (string.IsNullOrEmpty(tableCols))
                    {
                        Program.WriteLine();
                        Program.WriteLine($"Missing table definition for {config[i].masterDB}..{config[i].masterTableOrSP}", ConsoleColor.Red);
                        Program.ExitApp();
                    }

                }

                // get SP param list if needed
                var spParams = new DbAccess.ProcedureParts();
                if (templateContents.Contains("{4}") || templateContents.Contains("{5}") || templateContents.Contains("{6}"))
                {
                    spParams = DbAccess.GetProcedureParams(config[i].masterCS, config[i].masterTableOrSP);
                }

                // get a list of non-identity columns for insert statements, if needed
                string insertableColumnNames = "";
                if (templateContents.Contains("{7}"))
                {
                    insertableColumnNames = DbAccess.GetInsertableTableColumnNames(config[i].masterCS, config[i].masterTableOrSP);
                }

                // interpolate
                string outputContents = string.Format(templateContents, config[i].mirrorDB, config[i].masterDB, config[i].masterTableOrSP, tableCols, 
                    spParams.fullDef, spParams.listOfNames, spParams.selfAssignment, insertableColumnNames);

                string fileSuffix = string.Format(paramFileNameTemplate, config[i].mirrorDB, config[i].masterDB, config[i].masterTableOrSP);

                string outputFileName = $"{Path.GetFileNameWithoutExtension(templateFileName)}__{fileSuffix}{fileExtSQL}";

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }

        }

    }
}
