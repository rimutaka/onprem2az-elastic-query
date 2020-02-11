using System;
using Newtonsoft.Json;
using System.IO;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        /// <summary>
        /// Extract source SQL code for an object from a DB (-csl param) and save it in a local file. 
        /// </summary>
        /// <param name="configFile"></param>
        public static void ExtractScriptsFromDb(string paramCSLatest, string paramCSBase, string paramFileWithListOfItems)

        {
            // check input consistency
            if (string.IsNullOrEmpty(paramCSLatest))
            {
                Program.WriteLine();
                Program.WriteLine($"Connection string to a DB for script extraction is required. Use -csl param.", ConsoleColor.Red);
                Program.ExitApp();
            }

            if (string.IsNullOrEmpty(paramCSBase))
            {
                Program.WriteLine($"-csb param not supplied - extracting all objects from the list.", ConsoleColor.Yellow);
            }
            else
            {
                Program.WriteLine($"Extracting only objects that differ between -csl and -csb DBs.", ConsoleColor.Yellow);
            }

            string listOfObjectFileNames = null;

            // load the input file with the list of object names to extract (4-part file names)
            // or diff the HEAD with the very first commit to get the same list from GIT
            if (!string.IsNullOrEmpty(paramFileWithListOfItems))
            {
                Program.WriteLine($"Using a supplied list of file/object names to extract: {paramFileWithListOfItems}", ConsoleColor.Yellow);
                listOfObjectFileNames = LoadInputFile(paramFileWithListOfItems);
            }
            else
            {
                // find the ID of the initial commit
                string allCommits = GetGitOutput("rev-list HEAD") ?? "";
                string initialCommit = null;
                string[] allCommitsArray = allCommits.Split();
                Array.Reverse(allCommitsArray);
                foreach (string commit in allCommitsArray)
                {
                    if (commit.Length == 40)
                    {
                        // the first string from the bottom that looks like a 40-char SHA1 hash is our commit hash
                        initialCommit = commit;
                        break;
                    }
                }

                // do we have the commit ID?
                if (string.IsNullOrEmpty(initialCommit))
                {
                    Program.WriteLine();
                    Program.WriteLine($"Missing initial commit ID.", ConsoleColor.Red);
                    Program.ExitApp();
                }

                // get the list of GIT files changed since the first commit
                Program.WriteLine($"Getting the list of file/object names to extract from `git diff {initialCommit} HEAD`", ConsoleColor.Yellow);
                listOfObjectFileNames = GetGitOutput($"diff --name-only {initialCommit} HEAD") ?? "";

                // we should have some modified files here
                if (string.IsNullOrEmpty(listOfObjectFileNames))
                {
                    Program.WriteLine();
                    Program.WriteLine($"No modified files found in GIT.", ConsoleColor.Red);
                    Program.ExitApp();
                }
            }

            // process only the objects we are interested in
            foreach (string fileName in listOfObjectFileNames.Split())
            {
                if (!fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                {
                    Program.WriteLine($"Ignoring {fileName} - not an SQL script.", ConsoleColor.Yellow);
                    continue; // ignore any non-SQL files
                }

                // expecting a 4-part object name here, e.g. dbo.CR.StoredProcedure.sql
                string[] nameParts = fileName.Split('.');
                if (nameParts.Length != 4)
                {
                    Program.WriteLine($"Ignoring {fileName} - must be a 4-part name.", ConsoleColor.Yellow);
                    continue;
                }

                // get the SQL from syscomments for the object to be saved
                string sqlTextLatest = DbAccess.GetObjectText(paramCSLatest, nameParts[1]) ?? "";
                // get the SQL for the base DB object to compare to
                string sqlTextBase = (string.IsNullOrEmpty(paramCSBase)) ? "" : DbAccess.GetObjectText(paramCSBase, nameParts[1]) ?? "";

                // write out the file
                if (string.IsNullOrEmpty(paramCSBase) || sqlTextLatest != sqlTextBase)
                {
                    SaveExtractedScript(sqlTextLatest, fileName);
                }
                else
                {
                    Program.WriteLine($"Unchanged {fileName}");
                }
            }
        }
    }
}
