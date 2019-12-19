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
        public static void GenerateSqlCmdBatch(Configs.InitialConfig config, string targetDirectory)
        {
            // check if we have the server name
            string serverName = config.localServer;
            if (string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine();
                Console.WriteLine("Missing `localServer` param in `/config/config.json`");
                Program.ExitApp();
            }

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
                Console.WriteLine($"Empty folder: {targetDirectory}");
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
                    Console.WriteLine();
                    Console.WriteLine($"Cannot extract semantic parts from {fileNameOnly}");
                    Program.ExitApp();
                }
                string dbName = match.Groups[1]?.Value;

                sb.AppendLine($"sqlcmd -b -S {serverName} -d {dbName} -i \"{fileNameOnly}\"");
                sb.AppendLine($"if ($LASTEXITCODE -eq 0) {{git add \"{fileNameOnly}\"}}");
            }

            sb.AppendLine(); // an empty line at the end to execute the last statement

            // output file name
            string batFileName = Path.Combine(targetDirectory, "apply.ps1");


            // do not overwrite files for consistency
            if (File.Exists(batFileName))
            {
                Console.WriteLine($"#{batFileName} already exists.");
                Program.ExitApp(2);
            }

            Console.WriteLine($"Saving to {batFileName}");

            // save
            try
            {
                File.WriteAllText(batFileName, sb.ToString(), System.Text.Encoding.ASCII);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
