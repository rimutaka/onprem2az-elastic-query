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
        /// Load the specified template contents from a file or exits if the template cannot be found.
        /// </summary>
        /// <returns></returns>
        static string GetTemplateContents(string fileName)
        {
            string templateFolder = Path.Combine(Directory.GetCurrentDirectory(), Program.FileNames.TemplatesFolder);

            string templatePath = (Path.IsPathFullyQualified(fileName)) ? fileName : Path.Combine(templateFolder, fileName);

            if (!File.Exists(templatePath))
            {
                Program.WriteLine($"Template not found: {templatePath}", ConsoleColor.Red);
                Program.ExitApp();
            }

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
                Program.WriteLine();
                Program.WriteLine($"#{(i + 1).ToString()} - no destination folder", ConsoleColor.Red);
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
            outputFileName = Path.Combine(Program.FileNames.OutputFolder, outputFileName);
            string path = Path.Combine(Directory.GetCurrentDirectory(), outputFileName);

            // create the destination folder if it doesn't exist
            string scriptsFolder = Path.Combine(Directory.GetCurrentDirectory(), Program.FileNames.OutputFolder);
            if (!Directory.Exists(scriptsFolder)) Directory.CreateDirectory(scriptsFolder);

            // do not overwrite files for consistency
            if (File.Exists(path))
            {
                Program.WriteLine($"#{(i + 1).ToString()} - {outputFileName} already exists.", ConsoleColor.Yellow);
                return;
            }

            Program.WriteLine($"#{(i + 1).ToString()} - saving to {outputFileName}");

            try
            {
                File.WriteAllText(path, outputContents, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Program.WriteLine(ex.Message);
            }
        }


        /// <summary>
        /// Saves the contents in outputFileName in the working directory with overwrite
        /// </summary>
        /// <param name="outputContents"></param>
        /// <param name="outputFileName"></param>
        static void SaveExtractedScript(string outputContents, string outputFileName)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), outputFileName);

            Program.WriteLine($"Saving to {outputFileName}");

            try
            {
                File.WriteAllText(path, outputContents, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Program.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Load an input file with a list of items, one per line
        /// </summary>
        /// <param name="listFileName"></param>
        /// <returns></returns>
        static string LoadInputFile(string listFileName)
        {

            if (string.IsNullOrEmpty(listFileName))
            {
                Program.WriteLine();
                Program.WriteLine($"Use -l full_path or -l relative_path.", ConsoleColor.Red);
                Program.ExitApp();
            }

            // build the full path depending on the input
            string fullPath = (Path.IsPathRooted(listFileName)) ? listFileName : Path.Combine(Directory.GetCurrentDirectory(), listFileName);

            // check if the file exists
            if (!System.IO.File.Exists(fullPath))
            {
                Program.WriteLine();
                Program.WriteLine($"Missing list file: {fullPath}. Use -l full_path or -l relative_path.", ConsoleColor.Red);
                Program.ExitApp();
            }

            // load the file contents
            string fileContents = null;
            try
            {
                fileContents = File.ReadAllText(fullPath);
            }
            catch (Exception ex)
            {
                Program.WriteLine();
                Program.WriteLine("Cannot read the lines file: " + ex.Message, ConsoleColor.Red);
                Program.ExitApp();
            }

            if (string.IsNullOrEmpty(fileContents))
            {
                Program.WriteLine();
                Program.WriteLine($"The -l file {fullPath} is empty.", ConsoleColor.Red);
                Program.ExitApp();
            }

            return fileContents;
        }

        static void SaveFile(string outputContents, string outputFullName)
        {
            // do not overwrite files for consistency
            if (File.Exists(outputFullName))
            {
                Program.WriteLine($"#{outputFullName} already exists.", ConsoleColor.Yellow);
                Program.ExitApp(2);
            }

            Program.WriteLine($"Saving to {outputFullName}");

            try
            {
                File.WriteAllText(outputFullName, outputContents, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Program.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Get the mask for output file name depending on the value of paramRunOn
        /// </summary>
        /// <param name="paramRunOn"></param>
        /// <returns></returns>
        static string GetOutputFileNameMask(string paramRunOn)
        {
            switch (paramRunOn)
            {
                case "master":
                    {
                        return Program.FileNames.OutputFileNameMaskRunOnMaster;
                    }
                case "mirror":
                    {
                        return Program.FileNames.OutputFileNameMaskRunOnMirror;
                    }
            }

            Program.WriteLine();
            Program.WriteLine($"Missing parameter -o [master | mirror] to tell SqlCmd which db to run the script on.", ConsoleColor.Red);
            Program.ExitApp();

            return null; // this is really redundant, but keeps the lint happy

        }


        /// <summary>
        /// Runs git with the specified params in the current working dir and returns the output
        /// </summary>
        /// <param name="gitParams"></param>
        /// <returns></returns>
        static string GetGitOutput(string gitParams)
        {

            var cmdProc = new System.Diagnostics.Process();
            cmdProc.StartInfo.FileName = "git";
            cmdProc.StartInfo.Arguments = gitParams;
            cmdProc.StartInfo.UseShellExecute = false;
            cmdProc.StartInfo.RedirectStandardOutput = true;
            cmdProc.Start();

            string gitOutput = cmdProc.StandardOutput.ReadToEnd();

            cmdProc.WaitForExit();

            cmdProc.Dispose();

            return gitOutput;
        }
    }
}
