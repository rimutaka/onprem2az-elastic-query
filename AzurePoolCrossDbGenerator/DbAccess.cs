using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace AzurePoolCrossDbGenerator
{
    class DbAccess
    {

        const string trailingChars = "\r\n,"; // to normalise win/unix line endings

        /// <summary>
        /// Returns a short form of column definition, all nullable.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetTableColumns(string connectionString, string tableName)
        {
            // select column definitions from INFORMATION_SCHEMA.COLUMNS
            string queryString = @"select COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE 
                from INFORMATION_SCHEMA.COLUMNS
                where table_name = @table_name";

            System.Text.StringBuilder sb = new StringBuilder();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@table_name", tableName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        // read the column definition
                        string colName = reader["COLUMN_NAME"].ToString();
                        string colType = reader["DATA_TYPE"].ToString();
                        string colLen = reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        string colPrecision = reader["NUMERIC_PRECISION"].ToString();
                        string colScale = reader["NUMERIC_SCALE"].ToString();

                        // add to the list
                        sb.AppendLine(ItemDefinition(colName, colType, colLen, colPrecision, colScale));

                        // display a warning if incompatible data type was encountered
                        if (colType == "text" || colType == "image")
                        {
                            Program.WriteLine($"Incompatible type: {tableName}..{colName} {colType} must be fixed in the DB first.", ConsoleColor.Red);
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            // remove trailing , and new line
            return sb.ToString().TrimEnd(trailingChars.ToCharArray());
        }

        /// <summary>
        /// Returns a comma-separated list of table column names, excluding identity columns
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetInsertableTableColumnNames(string connectionString, string tableName)
        {
            // select column definitions from INFORMATION_SCHEMA.COLUMNS
            string queryString = @"select c.name from sys.all_columns c, sys.tables t 
                                    where c.object_id = t.object_id and t.name = @table_name and is_identity = 0
                                    order by column_id";

            StringBuilder sb = new StringBuilder();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@table_name", tableName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        // read the column definition
                        string colName = reader["name"].ToString();

                        // add to the list
                        sb.Append("[");
                        sb.Append(colName);
                        sb.Append("]");
                        sb.AppendLine(",");
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            // remove trailing , and new line
            return sb.ToString().TrimEnd(trailingChars.ToCharArray());
        }


        /// <summary>
        /// Returns a list of columns not supported by ElasticQuery for generating ALTER TABLE statements
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static List<ColumnDefinition> GetIncompatibleTableColumns(string connectionString, string tableName)
        {
            // select column definitions from INFORMATION_SCHEMA.COLUMNS
            string queryString = @"select COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE 
                from INFORMATION_SCHEMA.COLUMNS
                where table_name = @table_name";

            var outColumns = new List<ColumnDefinition>(); // output container

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@table_name", tableName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        // read the column definition
                        var colDef = new ColumnDefinition
                        {
                            colName = reader["COLUMN_NAME"].ToString(),
                            colType = reader["DATA_TYPE"].ToString(),
                            colLen = reader["CHARACTER_MAXIMUM_LENGTH"].ToString(),
                            colPrecision = reader["NUMERIC_PRECISION"].ToString(),
                            colScale = reader["NUMERIC_SCALE"].ToString()
                        };

                        // add columns of a matching type
                        if (string.Equals(colDef.colType, "text", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(colDef.colType, "image", StringComparison.OrdinalIgnoreCase)
                            )
                        {
                            outColumns.Add(colDef);
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            // remove trailing , and new line
            return outColumns;
        }

        /// <summary>
        /// Get lists of param names, param definitions and param assignments for building calls to remote SPs.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        public static ProcedureParts GetProcedureParams(string connectionString, string procedureName)
        {
            // query INFORMATION_SCHEMA.PARAMETERS for a list of params per procedure
            string queryString = @"select SPECIFIC_NAME, ORDINAL_POSITION, PARAMETER_MODE, PARAMETER_NAME,
	                DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
                from INFORMATION_SCHEMA.PARAMETERS
                where SPECIFIC_NAME = @procedureName order by ORDINAL_POSITION";

            // output containers
            var sbNames = new StringBuilder(); // contains names only
            var sbDefs = new StringBuilder(); // contains full definitions
            var sbAsn = new StringBuilder(); // contains self-assignments

            // get the list of params from the DB
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@procedureName", procedureName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    // check if there was any data returned at all
                    if (!reader.HasRows) return new ProcedureParts();

                    while (reader.Read())
                    {
                        // read the column definition
                        string parName = reader["PARAMETER_NAME"].ToString();
                        string parMode = reader["PARAMETER_MODE"].ToString();
                        string parType = reader["DATA_TYPE"].ToString();
                        string parLen = reader["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        string parPrecision = reader["NUMERIC_PRECISION"].ToString();
                        string parScale = reader["NUMERIC_SCALE"].ToString();

                        // only IN params are supported
                        if (parMode?.ToLower() != "in") Program.WriteLine($"Param {procedureName}.{parName} has invalid mode {parMode}", ConsoleColor.Red);

                        // add to the lists
                        sbNames.Append(parName);
                        sbNames.Append(",");

                        // e.g. @param1 nvarchar(255)
                        sbDefs.Append(ItemDefinition(parName, parType, parLen, parPrecision, parScale));

                        // e.g. @PPEId=@PPEId,
                        sbAsn.Append(parName);
                        sbAsn.Append("=");
                        sbAsn.Append(parName);
                        sbAsn.Append(",");
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            // build output parts in different formats
            var procParts = new ProcedureParts();

            // remove trailing , along the way
            procParts.listOfNames = "(" + sbNames.ToString().Trim(',') + ")";
            procParts.fullDef = sbDefs.ToString().Trim(',');
            procParts.selfAssignment = ",\n" + sbAsn.ToString().Trim(',');

            return procParts;
        }


        /// <summary>
        /// Returns TRUE if the SP exists in the target DB
        /// </summary>
        public static bool CheckProcedureExists(string connectionString, string procedureName)
        {
            // query INFORMATION_SCHEMA.PARAMETERS for a list of params per procedure
            string queryString = @"select SPECIFIC_NAME from INFORMATION_SCHEMA.ROUTINES where SPECIFIC_NAME = @procedureName";

            // get the list of params from the DB
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@procedureName", procedureName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();

                try
                {
                    if (reader.HasRows) return true;
                }
                finally
                {
                    reader.Close();
                }
            }

            return false;
        }


        /// <summary>
        /// Returns a formatted column or param definition, e.g. AgencyName nvarchar(255)
        /// </summary>
        static string ItemDefinition(string colName, string colType, string colLen, string colPrecision, string colScale)
        {
            // adjust long text types to (max)
            if (colType == "nvarchar" || colType == "varchar" || colType == "varbinary")
            {
                if (colLen == "-1" || long.Parse(colLen) > 8000) colLen = "max";
            }

            // image and text are not supported and have to be replaced
            if (colType == "image")
            {
                colType = "varbinary";
                colLen = "max";
            }
            if (colType == "text")
            {
                colType = "nvarchar";
                colLen = "max";
            }

            // text types have length - nvarchar(255)
            if (!String.IsNullOrEmpty(colLen)) colType += $"({colLen})";

            // numeric and decimal have precision
            if (!String.IsNullOrEmpty(colPrecision) && (colType == "numeric" || colType == "decimal")) colType += $"({colPrecision},{colScale})";

            // finalise the definition
            string colNull = "";
            if (!colName.Contains("@"))
            {
                colName = $"[{colName}]"; // enclose column names into [ ], leave @params as-is
                colNull = " NULL"; // is required for column definitions
            }
            string colDef = $"{colName} {colType}{colNull},";

            // double check there is no numeric/text params overlap
            if (!String.IsNullOrEmpty(colLen) && !String.IsNullOrEmpty(colPrecision))
                throw new Exception($"Invalid column definition ");

            return colDef;
        }


        /// <summary>
        /// Returns combined text from sys.syscomments for the object, assuming the object names are unique and no object is needed.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="objectName"></param>
        /// <returns></returns>
        public static string GetObjectText(string connectionString, string objectName)
        {
            // query sys.syscomments to get the source TSQL code for the DB object
            string queryString = $"select com.text, com.colid from sys.all_objects obj join sys.syscomments com on obj.object_id = com.id where name = @objectName order by com.colid";

            StringBuilder sb = new StringBuilder(); // output container for the SQL text

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.AddWithValue("@objectName", objectName);
                connection.Open();
                SqlDataReader reader = command.ExecuteReader();
                try
                {
                    // check if there was any data returned at all
                    if (!reader.HasRows) return null;

                    while (reader.Read())
                    {
                        // read the source SQL from `text` column
                        string sqlText = reader["text"].ToString();
                        sb.Append(sqlText);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get a connection string for the matching DB or log an error.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public static string GetConnectionString(Configs.InitialConfig config, string dbName)
        {
            string regexPattern = $".*Initial Catalog={dbName};.*";
            var regexOptions = RegexOptions.Multiline | RegexOptions.IgnoreCase;
            string cs = Regex.Match(config.connections, regexPattern, regexOptions)?.Value;
            if (string.IsNullOrEmpty(cs))
            {
                cs = "connection_string_required"; // a placeholder in case the CS is missing
                Program.WriteLine($"{config.mirrorDB} / {dbName}: missing connection string.", ConsoleColor.Red);
                Program.ExitApp();
            }

            return cs;
        }


        /// <summary>
        /// Get the DB name from the full connection string using regex
        /// </summary>
        /// <param name="dbConnectionString"></param>
        /// <returns></returns>
        public static string GetDbNameFromConnectionString(string dbConnectionString)
        {
            string regexPattern = @"Initial Catalog\s*=\s*([^;\s]+)";
            var regexOptions = RegexOptions.IgnoreCase;
            Match match = Regex.Match(dbConnectionString, regexPattern, regexOptions);
            if (!match.Success || match.Groups.Count<2 || string.IsNullOrEmpty(match.Groups[1].Value))
            {
                Program.WriteLine();
                Program.WriteLine("Cannot extract DB name from the connection string.", ConsoleColor.Red);
                Program.ExitApp();
            }

            return match.Groups[1].Value;
        }

        public class ProcedureParts
        {
            public string fullDef = "";
            public string listOfNames = "";
            public string selfAssignment = "";
        }

        public class ColumnDefinition
        {
            public string colName = "";
            public string colType = "";
            public string colLen = "";
            public string colPrecision = "";
            public string colScale = "";
        }
    }
}
