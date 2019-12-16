using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace AzurePoolCrossDbGenerator
{
    partial class Generators
    {

        #region "Constants"

        public static RegexOptions regexOptions_im = RegexOptions.IgnoreCase | RegexOptions.Multiline;

        /// <summary>
        /// E.g. ./citi_4vallees/dbo.ADD_MANUALRESERVATION_IN_CITI_STATS.StoredProcedure.sql:33:	INSERT INTO mr_CITI_STATS__TB_MANUALRESERVATION
        /// </summary>
        const string REGEX_GREP_PARTS = @"^(\.\/([^\/]*)[^:]*):(\d*):(.*)$";

        #endregion

        #region "Generic replacement functions"

        /// <summary>
        /// Replace all cross-DB refs and prepares a batch file for executing modified files with *SqlCmd* utility.
        /// </summary>
        public static void SearchAndReplace(string configJson, string changeListFileName, GetNew3PartObjectName getNew3PartObjectName)
        {
            string rootFolder = Path.GetDirectoryName(changeListFileName); // the input file should be in the root folder

            // .bat file name
            string batFileName = Path.GetFileNameWithoutExtension(changeListFileName) + ".ps1";
            batFileName = Path.Combine(rootFolder, batFileName);

            // do not overwrite files for consistency
            if (File.Exists(batFileName))
            {
                Console.WriteLine($"{batFileName} already exists.");
                Program.ExitApp(2);
            }

            // load config data
            Configs.SearchAndReplace config = JsonConvert.DeserializeObject<Configs.SearchAndReplace>(configJson);

            // check if we have the server name
            string serverName = config.localServer;
            if (string.IsNullOrEmpty(serverName))
            {
                Console.WriteLine("Missing `localServer` param in `/config/config.json`");
                Program.ExitApp();
            }

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
                var match = Regex.Match(inLine, REGEX_GREP_PARTS, regexOptions_im);
                if (!match.Success || match.Groups.Count != 5)
                {
                    Console.WriteLine($"Cannot extract semantic parts from line {inLineNumber}:\n {inLine}\n with {REGEX_GREP_PARTS}");
                    continue;
                }

                // get individual values
                string sqlFileName = match.Groups[1]?.Value;
                string dbName = match.Groups[2]?.Value;
                int lineNumber = (int.TryParse(match.Groups[3]?.Value, out lineNumber)) ? lineNumber - 1 : -1;
                string sqlStatement = match.Groups[4]?.Value;

                // check if any of the values are incorrect
                if (string.IsNullOrEmpty(sqlFileName) || string.IsNullOrEmpty(dbName) || string.IsNullOrEmpty(sqlStatement) || lineNumber < 0)
                {
                    Console.WriteLine($"Cannot extract semantic parts from this line:\n {inLine}\n with {REGEX_GREP_PARTS}");
                    continue;
                }

                // extract all 3 parts from the 3-part name
                string threePartRegex = $@"\[?(CITI_\w*)\]?\.\[?(\w*)\]?\.\[?(\w*)\]?";
                match = Regex.Match(sqlStatement, threePartRegex, regexOptions_im);
                if (!match.Success || match.Groups.Count != 4)
                {
                    Console.WriteLine($"Cannot extract semantic parts from line {inLineNumber}:\n {sqlStatement}\n with {threePartRegex}");
                    continue;
                }

                // prepare the new SQL statement
                string sqlObjectNameNew = getNew3PartObjectName(match.Groups[1]?.Value, match.Groups[2]?.Value, match.Groups[3]?.Value, config);
                string sqlStatementNew = Regex.Replace(sqlStatement, threePartRegex, sqlObjectNameNew, regexOptions_im);

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

                AddToBatFileList(sqlFileName, dbName, false, sqlFiles, sqlDBs, sqlFilesCommentOut);
            }

            // prepare .bat file

            var sb = new System.Text.StringBuilder();
            int rootPathLength = rootFolder.Length + 1;


            // loop thru the files
            for (int i = 0; i < sqlFiles.Count; i++)
            {
                string sqlCmdFileName = sqlFiles[i].Remove(0, rootPathLength);
                string gitFileName = Path.GetFileName(sqlFiles[i]);
                string commentOut = (sqlFilesCommentOut[i]) ? "#" : "";
                sb.AppendLine($"{commentOut}sqlcmd -b -S {serverName} -d {sqlDBs[i]} -i \"{sqlCmdFileName}\"");
                sb.AppendLine($"{commentOut}if ($LASTEXITCODE -eq 0) {{git -C {sqlDBs[i]} add \"{gitFileName}\"}}");
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
        static void AddToBatFileList(string sqlFileName, string dbName, bool CommentOut, List<string> sqlFiles, List<string> sqlDBs, List<bool> sqlFileCommentOut)
        {
            // save the file name for .bat generation
            if (!sqlFiles.Contains(sqlFileName))
            {
                sqlFiles.Add(sqlFileName);
                sqlDBs.Add(dbName.ToLower());
                sqlFileCommentOut.Add(CommentOut);
            }
        }

        #endregion

        #region "Replacement specific replacements"

        /// <summary>
        /// Returns a name of a mirror table for INSERT INTO replacement  
        /// </summary>
        public static string GetNewObjectNameInsert(string dbName, string schemaPart, string tablePart, Configs.SearchAndReplace config)
        {

            // prepare the new SQL object name
            string sqlObjectNameNew = string.Format(config.nameMirror, dbName, schemaPart, tablePart);

            return sqlObjectNameNew;
        }

        /// <summary>
        /// Returns a name of self-references replacement, e.g. mydb.dbo.mytable within mydb DB.
        /// </summary>
        public static string GetNewObjectNameSelfRefs(string dbName, string schemaPart, string tablePart, Configs.SearchAndReplace config)
        {

            // schemaPart can be .. or dbo.
            if (!string.IsNullOrEmpty(schemaPart)) schemaPart += ".";

            // prepare the new SQL object name
            string sqlObjectNameNew = string.Format(config.patternSelfRefs, dbName, schemaPart, tablePart);

            return sqlObjectNameNew;
        }

        #endregion

    }
}

