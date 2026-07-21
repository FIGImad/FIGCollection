using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class DBBrokerValidationRS : IDbEntity<DBBrokerValidationRS>
    {
        public string DatabaseName { get; set; }
        public string LoginName { get; set; }
        public bool Exists { get; set; }
        public bool Enabled { get; set; }
        public bool Permitted { get; set; }

        public DBBrokerValidationRS()
        {
            this.DatabaseName = string.Empty;
            this.LoginName = string.Empty;
            this.Exists = false;
            this.Enabled = false;
            this.Permitted = false;
        }

        public DBBrokerValidationRS(DBBrokerValidationRS rec)
        {
            this.DatabaseName = rec.DatabaseName;
            this.LoginName = rec.LoginName;
            this.Exists = rec.Exists;
            this.Enabled = rec.Enabled;
            this.Permitted = rec.Permitted;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
        }

        public DBBrokerValidationRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new DBBrokerValidationRS()
            {
                DatabaseName = SqlReaderUtil.GetString(reader, nSeq++),
                LoginName = SqlReaderUtil.GetString(reader, nSeq++),
                Exists = SqlReaderUtil.GetBoolean(reader, nSeq++),
                Enabled = SqlReaderUtil.GetBoolean(reader, nSeq++),
                Permitted = SqlReaderUtil.GetBoolean(reader, nSeq++)
            };
            return rec;
        }

    }
}
