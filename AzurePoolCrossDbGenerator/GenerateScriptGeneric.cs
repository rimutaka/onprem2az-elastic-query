using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Use an arbitrary template to generate an SQL script. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void GenerateScript(Configs.InitialConfig config, string templateFileName)

        {
            string templateContents = Generators.GetTemplateContents(templateFileName);

            // get the list of all placeholders in the template
            var matches = Regex.Matches(templateContents, @"{{\s*([^}]+)\s*}}");
            var matchPlaceholders = new List<string>(); // contains replacement placeholders like {{loggingAzStorageContainerUrl}}

            // process placeholders one by one
            foreach (Match match in matches)
            {
                string placeholder = match.Value;
                if (matchPlaceholders.Contains(placeholder)) continue; // ignore repeating placeholders

                matchPlaceholders.Add(placeholder); // add it to the list so we don't process it multiple times

                try
                {
                    // replace all instances with the value from config
                    string matchValue = (string)config.GetType().GetField(match.Groups[1].Value).GetValue(config);
                    templateContents = templateContents.Replace(placeholder, matchValue, StringComparison.Ordinal);
                }
                catch (Exception ex)
                {
                    Program.WriteLine();
                    Program.WriteLine($"Variable has no matching config entry: {placeholder}.", ConsoleColor.Red);
                    Program.ExitApp();
                }
            }

            string fileSuffix = string.Format(Program.FileNames.OutputFileNameMaskRunOnMirror, config.mirrorDB, "x", "x");

            string outputFileName = $"{Path.GetFileNameWithoutExtension(templateFileName)}__{fileSuffix}{fileExtSQL}";

            Generators.SaveGeneratedScript(templateContents, outputFileName, 0);
        }
    }
}

