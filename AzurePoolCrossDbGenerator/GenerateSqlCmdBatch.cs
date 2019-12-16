using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Generate a .bat file for executing all SQL scripts in the current directory. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void GenerateSqlCmdBatch(string configJson, string targetDirectory)
        {
            // load config data
            Configs.InitialConfig config = JsonConvert.DeserializeObject<Configs.InitialConfig>(configJson);

            // check if we have the server name
            string serverName = config.localServer;
            if (string.IsNullOrEmpty(serverName))
            {
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

            var sb = new System.Text.StringBuilder();

            // loop thru the files
            foreach (string fileName in Directory.GetFiles(targetDirectory))
            {

                if (fileName.EndsWith(fileExtSQL))
                {
                    string fileNameOnly = Path.GetFileName(fileName);
                    sb.AppendLine($"sqlcmd -b -S {serverName} -i \"{fileNameOnly}\"");
                    sb.AppendLine($"if ($LASTEXITCODE -eq 0) {{git add \"{fileNameOnly}\"}}");
                }
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
