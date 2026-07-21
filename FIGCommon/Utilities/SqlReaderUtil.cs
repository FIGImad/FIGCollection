using Microsoft.Data.SqlClient;

namespace FIGCommon.Utilities
{
    public class SqlReaderUtil
    {
        #region RetrievalMethods

        public static string GetString(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? "" : reader.GetString(ndx);
        }

        public static string? GetNullableString(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (string?)null : reader.GetString(ndx);
        }

        public static bool GetBoolean(SqlDataReader reader, int ndx)
        {
            return !reader.IsDBNull(ndx) && reader.GetBoolean(ndx);
        }

        public static bool? GetNullableBoolean(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (bool?)null : reader.GetBoolean(ndx);
        }

        public static int GetInt32(SqlDataReader reader, int ndx)
        {
            if (!reader.IsDBNull(ndx))
            {
                if (reader.GetDataTypeName(ndx) == "float")
                {
                    return (int)reader.GetDouble(ndx);
                }
            }
            return reader.IsDBNull(ndx) ? 0 : reader.GetInt32(ndx);
        }

        public static int? GetNullableInt32(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (int?)null : reader.GetInt32(ndx);
        }


        public static long GetInt64(SqlDataReader reader, int ndx)
        {
            if (!reader.IsDBNull(ndx))
            {
                if (reader.GetDataTypeName(ndx) == "float")
                {
                    return (int)reader.GetDouble(ndx);
                }
            }
            return reader.IsDBNull(ndx) ? 0 : reader.GetInt64(ndx);
        }

        public static long? GetNullableInt64(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (long?)null : reader.GetInt64(ndx);
        }

        public static decimal GetDecimal(SqlDataReader reader, int ndx)
        {
            if (!reader.IsDBNull(ndx))
            {
                if (reader.GetDataTypeName(ndx) == "float")
                {
                    return (decimal)reader.GetDouble(ndx);
                }
            }
            return reader.IsDBNull(ndx) ? 0 : reader.GetDecimal(ndx);
        }
        public static decimal? GetNullableDecimal(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (decimal?)null : reader.GetDecimal(ndx);
        }


        public static double GetDouble(SqlDataReader reader, int ndx)
        {
            if (!reader.IsDBNull(ndx))
            {
                if (reader.GetDataTypeName(ndx) == "float")
                {
                    return (double)reader.GetDouble(ndx);
                }
            }
            return reader.IsDBNull(ndx) ? 0 : reader.GetDouble(ndx);
        }

        public static double? GetNullableDouble(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (double?)null : reader.GetDouble(ndx);
        }

        public static DateTimeOffset GetDateTimeOffset(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? DateTimeOffset.MinValue : reader.GetDateTimeOffset(ndx);
        }

        public static DateTimeOffset? GetNullableDateTimeOffset(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (DateTimeOffset?)null : reader.GetDateTimeOffset(ndx);
        }

        public static DateTime GetDateTime(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? DateTime.MinValue : reader.GetDateTime(ndx);
        }

        public static DateTime? GetNullableDateTime(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? (DateTime?)null : reader.GetDateTime(ndx);
        }

        public static object GetValue(SqlDataReader reader, int ndx)
        {
            return reader.IsDBNull(ndx) ? 0 : reader.GetValue(ndx);
        }

        public static string GetName(SqlDataReader reader, int ndx)
        {
            if (!reader.IsDBNull(ndx))
            {
                return reader.GetName(ndx);
            }
            return "";
        }


        #endregion RetrievalMethods

    }
}
