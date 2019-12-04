using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    /// <summary>
    /// A collection of functions for generating specific scripts
    /// </summary>
    partial class Generators
    {
        public const string fileExtSQL = ".sql";

        /// <summary>
        /// Load the specified template contents from a file
        /// </summary>
        /// <param name="templateFolder"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        static string GetTemplateContents(string templateFolder, string fileName)
        {
            string templatePath = Path.Combine(templateFolder, fileName);
            string templateContents = File.ReadAllText(templatePath);

            return templateContents;
        }

        /// <summary>
        /// Merges the config items and cheks if the destination folder exists.
        /// </summary>
        /// <returns></returns>
        static bool IsDestFolderOK(string folder, int i)
        {
            // check if the destination file was specified
            if (string.IsNullOrEmpty(folder))
            {
                Console.WriteLine($"#{(i + 1).ToString()} - no destination folder");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write out the file and log the result to console
        /// </summary>
        /// <param name="outputContents"></param>
        /// <param name="outputFileName"></param>
        /// <param name="i"></param>
        static void SaveGeneratedScript(string outputContents, string outputFileName, int i)
        {
            Console.WriteLine($"#{(i + 1).ToString()} - saving to {outputFileName}");

            try
            {
                File.WriteAllText(outputFileName, outputContents, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
