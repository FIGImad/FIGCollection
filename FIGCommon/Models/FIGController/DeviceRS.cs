using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;


namespace FIGCommon.Models
{
    public class DeviceRS : IDbEntity<DeviceRS>
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string ServiceName { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string IPAddress { get; set; } = "";
        public int Role { get; set; } = 0;
        public string Version { get; set; } = "";
        public string ConnectionId { get; set; } = "";
        public int RegistrationTime { get; set; } = 0;
        public int LastUpdatedTime { get; set; } = 0;
        public bool Enabled { get; set; } = true;

        public DeviceRS() { }

        public DeviceRS(DeviceRS data)
        {
            Clone(data);
        }

        public void Clone(DeviceRS src)
        {
            var srcT = src.GetType();
            var dstT = this.GetType();
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(this, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(this, f.GetValue(src, null), null);
            }
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@ServiceName", ServiceName);
            parameters.AddWithValue("@ServiceType", ServiceType);
            parameters.AddWithValue("@MachineName", MachineName);
            parameters.AddWithValue("@IPAddress", IPAddress);
            parameters.AddWithValue("@Role", Role);
            parameters.AddWithValue("@Version", Version);
            parameters.AddWithValue("@ConnectionId", ConnectionId);
            parameters.AddWithValue("@RegistrationTime", RegistrationTime);
            parameters.AddWithValue("@LastUpdatedTime", LastUpdatedTime);
            parameters.AddWithValue("@Enabled", Enabled);
        }

        public DeviceRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            DeviceRS rec = new DeviceRS()
            {
                Id = SqlReaderUtil.GetString(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                ServiceName = SqlReaderUtil.GetString(reader, nSeq++),
                ServiceType = SqlReaderUtil.GetString(reader, nSeq++),
                MachineName = SqlReaderUtil.GetString(reader, nSeq++),
                IPAddress = SqlReaderUtil.GetString(reader, nSeq++),
                Role = SqlReaderUtil.GetInt32(reader, nSeq++),
                Version = SqlReaderUtil.GetString(reader, nSeq++),
                ConnectionId = SqlReaderUtil.GetString(reader, nSeq++),
                RegistrationTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                LastUpdatedTime = SqlReaderUtil.GetInt32(reader, nSeq++),
                Enabled = SqlReaderUtil.GetBoolean(reader, nSeq++)
            };
            return rec;
        }
    }
}
