using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class ArchivedSignalRS : IDbEntity<ArchivedSignalRS>
    {
        public int AutoTradeCount { get; set; } = 0;
        public string SignalTag { get; set; } = "";
        public string Strategy { get; set; } = "";
        public decimal Side { get; set; } = 0m;
        public long OpenTime { get; set; } = 0;
        public long CloseTime { get; set; } = 0;

        public ArchivedSignalRS() { }

        public void ToSqlCommandParameters(SqlParameterCollection parameters) { }

        public ArchivedSignalRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            return new ArchivedSignalRS
            {
                AutoTradeCount = SqlReaderUtil.GetInt32(reader, nSeq++),
                SignalTag      = SqlReaderUtil.GetString(reader, nSeq++),
                Strategy       = SqlReaderUtil.GetString(reader, nSeq++),
                Side           = SqlReaderUtil.GetDecimal(reader, nSeq++),
                OpenTime       = (long)SqlReaderUtil.GetInt32(reader, nSeq++),
                CloseTime      = (long)SqlReaderUtil.GetInt32(reader, nSeq++)
            };
        }
    }
}
