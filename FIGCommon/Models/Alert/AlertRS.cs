using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models.Alert
{
    public class AlertRS : IDbEntity<AlertRS>
    {
        public int Id { get; set; }
        public int RawTime { get; set; }
        public int MSec { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int ServiceRole { get; set; }
        public string ServiceAddress { get; set; }
        public string LogName { get; set; }
        public string Source { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string? ExceptionText { get; set; }

        public AlertRS()
        {
            this.Id = -1;
            this.RawTime = 0;
            this.MSec = 0;
            this.ServiceId = "";
            this.ServiceName = "";
            this.ServiceRole = 0;
            this.ServiceAddress = "";
            this.LogName = "";
            this.Source = "";
            this.Level = "";
            this.Message = "";
            this.ExceptionText = null;
        }

        public AlertRS(AlertRS rec)
        {
            this.Id = rec.Id;
            this.RawTime = rec.RawTime;
            this.MSec = rec.MSec;
            this.ServiceId = rec.ServiceId;
            this.ServiceName = rec.ServiceName;
            this.ServiceRole = rec.ServiceRole;
            this.ServiceAddress = rec.ServiceAddress;
            this.LogName = rec.LogName;
            this.Source = rec.Source;
            this.Level = rec.Level;
            this.Message = rec.Message;
            this.ExceptionText = rec.ExceptionText;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@RawTime", RawTime);
            parameters.AddWithValue("@MSec", MSec);
            parameters.AddWithValue("@ServiceId", ServiceId);
            parameters.AddWithValue("@ServiceName", ServiceName);
            parameters.AddWithValue("@ServiceRole", ServiceRole);
            parameters.AddWithValue("@ServiceAddress", ServiceAddress);
            parameters.AddWithValue("@LogName", LogName);
            parameters.AddWithValue("@Source", Source);
            parameters.AddWithValue("@Level", Level);
            parameters.AddWithValue("@Message", Message);
            parameters.AddWithValue("@ExceptionText", (object?)ExceptionText ?? DBNull.Value);
        }

        public AlertRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new AlertRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                RawTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                MSec = SqlReaderUtil.GetInt32(reader, nSeq++),
                ServiceId = SqlReaderUtil.GetString(reader, nSeq++),
                ServiceName = SqlReaderUtil.GetString(reader, nSeq++),
                ServiceRole = SqlReaderUtil.GetInt32(reader, nSeq++),
                ServiceAddress = SqlReaderUtil.GetString(reader, nSeq++),
                LogName = SqlReaderUtil.GetString(reader, nSeq++),
                Source = SqlReaderUtil.GetString(reader, nSeq++),
                Level = SqlReaderUtil.GetString(reader, nSeq++),
                Message = SqlReaderUtil.GetString(reader, nSeq++),
                ExceptionText = SqlReaderUtil.GetNullableString(reader, nSeq++)
            };
            return rec;
        }
    }
}
