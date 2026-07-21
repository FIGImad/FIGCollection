using Microsoft.Data.SqlClient;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public static class SignalStatus
    {
        public static readonly string Start = "START";
        public static readonly string Stop = "STOP";
    }

    public class SignalRS : IDbEntity<SignalRS>
    {
        public int Id { get; set; } = -1;
        public string Strategy { get; set; } = "";
        public string Tag { get; set; } = "";
        public string Status { get; set; } = "";
        public decimal Side { get; set; } = 0m;

        public long? StartTime { get; set; }
        public long? StopTime { get; set; }

        public decimal? StartPrice { get; set; }
        public decimal? StopPrice { get; set; }
        public bool Canceled { get; set; } = false;

        public long LastUpdated { get; set; } = -1;

        public StrategyConfigRS? StrategyConfig { get; set; } = null;

        public SignalRS() { }
        public SignalRS(SignalRS data) => Clone(data);

        public void Clone(SignalRS src)
        {
            var srcT = src.GetType();
            var dstT = GetType();

            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral) continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var p in srcT.GetProperties())
            {
                var dstP = dstT.GetProperty(p.Name);
                if (dstP == null) continue;
                dstP.SetValue(this, p.GetValue(src, null), null);
            }

            StrategyConfig = null;
            if (src.StrategyConfig != null)
            {
                this.StrategyConfig = new StrategyConfigRS(src.StrategyConfig);
            }
            

        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            // Match table types and allow NULLs
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Strategy", Strategy);
            parameters.AddWithValue("@Tag", Tag);
            parameters.AddWithValue("@Status", Status);
            parameters.AddWithValue("@Side", Side);

            parameters.AddWithValue("@StartTime", (object?)StartTime ?? DBNull.Value);
            parameters.AddWithValue("@StopTime", (object?)StopTime ?? DBNull.Value);

            parameters.AddWithValue("@StartPrice", (object?)StartPrice ?? DBNull.Value);
            parameters.AddWithValue("@StopPrice", (object?)StopPrice ?? DBNull.Value);

            parameters.AddWithValue("@Canceled", Canceled);

            parameters.AddWithValue("@LastUpdated", LastUpdated);
        }

        public SignalRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;

            // Assumes your SELECT column order matches the table order shown.
            // If you SELECT in a different order, adjust the sequence accordingly.
            return new SignalRS
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Strategy = SqlReaderUtil.GetString(reader, nSeq++),
                Tag = SqlReaderUtil.GetString(reader, nSeq++),
                Status = SqlReaderUtil.GetString(reader, nSeq++),
                Side = SqlReaderUtil.GetDecimal(reader, nSeq++),

                StartTime = (long?) SqlReaderUtil.GetNullableInt32(reader, nSeq++),
                StopTime = (long?) SqlReaderUtil.GetNullableInt32(reader, nSeq++),

                StartPrice = SqlReaderUtil.GetNullableDecimal(reader, nSeq++),
                StopPrice = SqlReaderUtil.GetNullableDecimal(reader, nSeq++),

                Canceled = SqlReaderUtil.GetBoolean(reader, nSeq++),

                LastUpdated = (long)SqlReaderUtil.GetInt32(reader, nSeq++)
            };
        }
    }
}
