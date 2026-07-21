using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class SettingRS : IDbEntity<SettingRS>
    {
        public int Id { get; set; }
        public string Group { get; set; }
        public string Tag { get; set; }
        public string Value { get; set; }

        public SettingRS()
        {
            this.Id = -1;
            this.Group = "";
            this.Tag = "";
            this.Value = "";
        }

        public SettingRS(SettingRS rec)
        {
            this.Id = rec.Id;
            this.Group = rec.Group;
            this.Tag = rec.Tag;
            this.Value = rec.Value;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Group", Group);
            parameters.AddWithValue("@Tag", Tag);
            parameters.AddWithValue("@Value", Value);
        }

        public SettingRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new SettingRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Group = SqlReaderUtil.GetString(reader, nSeq++),
                Tag = SqlReaderUtil.GetString(reader, nSeq++),
                Value = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

    }
}
