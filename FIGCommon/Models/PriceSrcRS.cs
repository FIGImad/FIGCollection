using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class PriceSrcRS : IDbEntity<PriceSrcRS>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public PriceSrcRS()
        {
            this.Id = -1;
            this.Name = "";
            this.Value = "";
        }

        public PriceSrcRS(PriceSrcRS rec)
        {
            this.Id = rec.Id;
            this.Name = rec.Name;
            this.Value = rec.Value;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@Name", Name);
            parameters.AddWithValue("@Value", Value);
        }

        public PriceSrcRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new PriceSrcRS()
            {
                Id = SqlReaderUtil.GetInt32(reader, nSeq++),
                Name = SqlReaderUtil.GetString(reader, nSeq++),
                Value = SqlReaderUtil.GetString(reader, nSeq++)
            };
            return rec;
        }

    }
}

