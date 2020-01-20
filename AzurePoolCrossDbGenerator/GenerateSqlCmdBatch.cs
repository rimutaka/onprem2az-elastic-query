using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Generate a .bat file for executing all SQL scripts in the current directory. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void GenerateSqlCmdBatch(Configs.InitialConfig config, string targetDirectory, string oParam)
        {
            // check if we have the server name
            bool runOnAz = string.Equals(oParam, "az", StringComparison.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(oParam) && !runOnAz)
            {
                Program.WriteLine();
                Program.WriteLine("Param -o can be set to AZ (run on Azure) or omitted (run locally).", ConsoleColor.Red);
                Program.ExitApp();
            }

            // check if we have the server name
            string serverName = (runOnAz)? config.serverName + ".database.windows.net" : config.localServer;
            if (string.IsNullOrEmpty(serverName))
            {
                string serverParamName = (runOnAz) ? "serverName" : "localServer";
                Program.WriteLine();
                Program.WriteLine($"Missing `{serverParamName}` param in `/config/config.json`", ConsoleColor.Red);
                Program.ExitApp();
            }

            // prepare identity (user name) for Azure
            string userName = (runOnAz) ? $"-U '{config.identity}'" : "";

            // the target directory can be relative or absolute
            if (!Path.IsPathRooted(targetDirectory))
            {
                targetDirectory = targetDirectory.TrimStart(('\\'));
                if (!targetDirectory.ToLower().StartsWith("scripts\\")) targetDirectory = "scripts\\" + targetDirectory;
                targetDirectory = Path.Combine(Directory.GetCurrentDirectory(), targetDirectory);
            }

            // get all files in the folder
            string[] fileNames = Directory.GetFiles(targetDirectory);

            // do not write out an empty file
            if (fileNames.Length == 0)
            {
                Program.WriteLine($"Empty folder: {targetDirectory}", ConsoleColor.Yellow);
                Program.ExitApp(2);
            }

            

            // loop thru the files
            var sb = new System.Text.StringBuilder();
            foreach (string fileName in fileNames)
            {
                // skip non-.sql files
                if (!fileName.EndsWith(fileExtSQL)) continue;

                string fileNameOnly = Path.GetFileName(fileName);

                // extract the DB to run the script in from the file name
                var match = Regex.Match(fileNameOnly, @"^.*__([\w\d]*)__[\w\d]*__[\w\d]*", regexOptions_im);
                if (!match.Success || match.Groups.Count != 2)
                {
                    Program.WriteLine();
                    Program.WriteLine($"Cannot extract semantic parts from {fileNameOnly}", ConsoleColor.Red);
                    Program.ExitApp();
                }
                string dbName = match.Groups[1]?.Value;

                sb.AppendLine($"sqlcmd -b -S \"{serverName}\" {userName} -d {dbName} -i \"{fileNameOnly}\"");
                sb.AppendLine($"if ($LASTEXITCODE -eq 0) {{git add \"{fileNameOnly}\"}}");
            }

            sb.AppendLine(); // an empty line at the end to execute the last statement

            // output file name
            string batFileName = Path.Combine(targetDirectory, "apply.ps1");


            // do not overwrite files for consistency
            if (File.Exists(batFileName))
            {
                Program.WriteLine($"#{batFileName} already exists.", ConsoleColor.Yellow);
                Program.ExitApp(2);
            }

            Program.WriteLine($"Saving to {batFileName}");

            // save
            try
            {
                File.WriteAllText(batFileName, sb.ToString(), System.Text.Encoding.ASCII);
            }
            catch (Exception ex)
            {
                Program.WriteLine(ex.Message);
            }
        }
    }
}
