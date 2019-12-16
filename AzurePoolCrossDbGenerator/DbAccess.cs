using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace AzurePoolCrossDbGenerator
{
    class DbAccess
    {
        /// <summary>
        /// Returns a short form of column definition, all nullable.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public static string GetTableColumns(string connectionString, string tableName)
        {
            const string trailingChars = "\r\n,";
            List<String> numericDataTypes = new List<string>();
            numericDataTypes.AddRange(new string[] { "numeric", "decimal" });

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

                        // adjust long text types to (max)
                        if (colType == "nvarchar" || colType == "varchar")
                        {
                            if (colLen == "-1" || long.Parse(colLen) > 8000) colLen = "max";
                        }

                        // image and text have length, but not in the SQL definitions
                        if (colType == "image" || colType == "text") colLen = "";

                        // text types have length - nvarchar(255)
                        if (!String.IsNullOrEmpty(colLen)) colType += $"({colLen})";

                        // numeric and decimal have precision
                        if (!String.IsNullOrEmpty(colPrecision) && numericDataTypes.Contains(colType)) colType += $"({colPrecision},{colScale})";

                        // finalise the definition
                        string colDef = $"[{colName}] {colType} NULL,";

                        // double check there is no numeric/text params overlap
                        if (!String.IsNullOrEmpty(colLen) && !String.IsNullOrEmpty(colPrecision))
                            throw new Exception($"Invalid column definition ");

                        // add to the list
                        sb.AppendLine(colDef);
                    }
                }
                finally
                {
                    reader.Close();
                }
            }

            // remote trailing , and new line
            return sb.ToString().TrimEnd(trailingChars.ToCharArray());
        }
    }
}
