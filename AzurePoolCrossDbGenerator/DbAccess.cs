using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

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
                        if (colType=="text" || colType == "image")
                        {
                            Program.WriteLine($"Incompatible type: {tableName}..{colName} {colType}", ConsoleColor.Red);
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
                        if (parMode?.ToLower()!="in") Program.WriteLine($"Param {procedureName}.{parName} has invalid mode {parMode}", ConsoleColor.Red);

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
            if (colType == "nvarchar" || colType == "varchar")
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
