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

            var sb = new System.Text.StringBuilder();

            // loop thru the files
            foreach (string fileName in Directory.GetFiles(targetDirectory))
            {

                if (fileName.EndsWith(fileExtSQL))
                {
                    sb.AppendLine($"sqlcmd -S {serverName} -i \"{Path.GetFileName(fileName)}\"");
                }
            }

            sb.AppendLine(); // an empty line at the end to execute the last statement

            // output file name
            string batFileName = Path.Combine(targetDirectory, "apply.bat");


            // do not overwrite files for consistency
            if (File.Exists(batFileName))
            {
                Console.WriteLine($"#{batFileName} already exists.");
                Program.ExitApp();
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
