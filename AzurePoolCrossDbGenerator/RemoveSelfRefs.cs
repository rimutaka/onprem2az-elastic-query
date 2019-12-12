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
        /// Removes all DB self-references and prepares a batch file for executing modified files with *SqlCmd* utility.
        /// </summary>
        public static void RemoveSelfRefs(string configJson, string changeListFileName)
        {
            string rootFolder = Path.GetDirectoryName(changeListFileName); // the input file should be in the root folder

            // .bat file name
            string batFileName = Path.GetFileNameWithoutExtension(changeListFileName) + ".bat";
            batFileName = Path.Combine(rootFolder, batFileName);

            // do not overwrite files for consistency
            if (File.Exists(batFileName))
            {
                Console.WriteLine($"{batFileName} already exists.");
                Program.ExitApp();
            }

            // load config data
            Configs.InitialConfig config = JsonConvert.DeserializeObject<Configs.InitialConfig>(configJson);

            // check if we have the server name
            string serverName = config.localServer;
            if (string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine("Missing `localServer` param in `/config/config.json`");
                Program.ExitApp();
            }

            // shared regex options
            const string REGEX_PARTS = @"^(\.\/([^\/]*)[^:]*):(\d*):(.*)$";
            RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline;

            // list of all files and DBs to add to SQL CMD
            List<string> sqlFiles = new List<string>();
            List<string> sqlDBs = new List<string>();
            List<bool> sqlFilesCommentOut = new List<bool>();
            int inLineNumber = 0;

            // load the change list
            foreach (string inLine in File.ReadAllLines(changeListFileName))
            {
                inLineNumber++;
                if (string.IsNullOrWhiteSpace(inLine)) continue; // skip empty lines

                // get all the parts of the string
                // e.g. ./citi_ip_country/dbo.GetRentalpCountryCodeByIpNumber.UserDefinedFunction.sql:18:	from	citi_ip_country..tb_ip p, citi_ip_country..tb_location c
                var match = Regex.Match(inLine, REGEX_PARTS, regexOptions);
                if (!match.Success || match.Groups.Count != 5)
                {
                    Console.WriteLine($"Cannot extract semantic parts from line {inLineNumber}:\n {inLine}\n with {REGEX_PARTS}");
                    continue;
                }

                // get individual values
                string sqlFileName = match.Groups[1]?.Value?.ToLower();
                string dbName = match.Groups[2]?.Value;
                int lineNumber = (int.TryParse(match.Groups[3]?.Value, out lineNumber)) ? lineNumber - 1 : -1;
                string sqlStatement = match.Groups[4]?.Value;

                // check if any of the values are incorrect
                if (string.IsNullOrEmpty(sqlFileName) || string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(sqlStatement) || lineNumber < 0)
                {
                    Console.WriteLine($"Cannot extract semantic parts from this line:\n {inLine}\n with {REGEX_PARTS}");
                    continue;
                }

                // prepare the replacement line
                string replaceRegex = $@"\[?{dbName}\]?\.\[?(?:dbo)?\]?\.(?=\[?(\w*)\]?)";
                string sqlStatementNew = Regex.Replace(sqlStatement, replaceRegex, "", regexOptions);

                // log the output
                Console.WriteLine();
                Console.WriteLine($"#{inLineNumber}  {sqlFileName}");
                Console.WriteLine(sqlStatement);
                Console.WriteLine(sqlStatementNew);

                // load the file and find the matching line match
                sqlFileName = sqlFileName.Replace("/", "\\").TrimStart(new char[] { '.', '\\' });
                sqlFileName = Path.Combine(rootFolder, sqlFileName);
                string[] sqlLines = File.ReadAllLines(sqlFileName);

                // check if the line number is valid
                if (sqlLines.Length <= lineNumber)
                {
                    Console.WriteLine($"Line {lineNumber + 1} is out of bounds.");
                    continue;
                }

                // check if the file line matches the new line
                if (sqlLines[lineNumber] == sqlStatementNew)
                {
                    Console.WriteLine($"Already modified.");
                    AddToBatFileList(sqlFileName, dbName, true, sqlFiles, sqlDBs, sqlFilesCommentOut);
                    continue;
                }

                // check if the line matches the old line
                if (sqlLines[lineNumber] != sqlStatement)
                {
                    Console.WriteLine(sqlLines[lineNumber]);
                    Console.WriteLine($"Line {lineNumber + 1} mismatch in the SQL file.");
                    continue;
                }

                // replace the line
                sqlLines[lineNumber] = sqlStatementNew;

                // write out the file
                File.WriteAllLines(sqlFileName, sqlLines, System.Text.Encoding.UTF8);

                AddToBatFileList(sqlFileName, dbName, false, sqlFiles, sqlDBs,sqlFilesCommentOut);
            }

            // prepare .bat file

            var sb = new System.Text.StringBuilder();

            // loop thru the files
            for (int i = 0; i < sqlFiles.Count; i++)
            {
                string commentOut = (sqlFilesCommentOut[i]) ? " REM " : "";
                    sb.AppendLine($"{commentOut}sqlcmd -S {serverName} -d {sqlDBs[i]} -i \"{sqlFiles[i]}\"");
            }

            sb.AppendLine(); // an empty line at the end to execute the last statement

            Console.WriteLine();
            Console.WriteLine($"Saving SQLCMD to {batFileName}");

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

        /// <summary>
        /// Save the file and DB name for SQLCMD generation
        /// </summary>
        /// <param name="sqlFileName"></param>
        /// <param name="dbName"></param>
        /// <param name="sqlFiles"></param>
        /// <param name="sqlDBs"></param>
        static void AddToBatFileList (string sqlFileName, string dbName, bool CommentOut, List<string> sqlFiles, List<string> sqlDBs, List<bool> sqlFileCommentOut)
        {
            // save the file name for .bat generation
            if (!sqlFiles.Contains(sqlFileName))
            {
                sqlFiles.Add(sqlFileName);
                sqlDBs.Add(dbName.ToLower());
                sqlFileCommentOut.Add(CommentOut);
            }
        }

    }
}

