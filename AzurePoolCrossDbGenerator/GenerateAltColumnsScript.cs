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
        public static void GenerateAltColumnsScript(Configs.AllTables[] config)

        {
            // load config data
            Configs.AllTables sharedConfig = new Configs.AllTables();

            // generate output one file at a time
            for (int i = 0; i < config.Length; i++)
            {
                // merge with the previous full version of the config
                sharedConfig = (Configs.AllTables)config[i].Merge(sharedConfig);

                // get the column list if there is {3} group in the template
                var tableCols = DbAccess.GetIncompatibleTableColumns(config[i].masterCS, config[i].masterTableOrSP);

                var sb = new System.Text.StringBuilder();

                // generate one script file 
                foreach (DbAccess.ColumnDefinition colDef in tableCols)
                {
                    string altLine = "";
                    switch (colDef.colType.ToLower())
                    {
                        case "image":
                            {
                                altLine = $"ALTER TABLE {config[i].masterTableOrSP} ALTER COLUMN {colDef.colName} varbinary(max)\nGO";
                                break;
                            }
                        case "text":
                            {
                                altLine = $"ALTER TABLE {config[i].masterTableOrSP} ALTER COLUMN {colDef.colName} nvarchar(max)\nGO";
                                break;
                            }
                        default:
                            {
                                // do nothing?
                                break;
                            }
                    }

                    sb.AppendLine(altLine);
                }

                // are there any columns to alter at all?
                if (sb.Length == 0) continue;

                // save one file per table
                string outputContents = sb.ToString();

                string fileSuffix = string.Format(Program.FileNames.OutputFileNameMaskRunOnMaster, "x", config[i].masterDB, config[i].masterTableOrSP);

                string outputFileName = $"AlterTableColumns__{fileSuffix}{fileExtSQL}";

                Generators.SaveGeneratedScript(outputContents, outputFileName, i);
            }

        }

    }
}
