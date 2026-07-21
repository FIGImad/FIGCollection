using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class DBUserStatusRS : IDbEntity<DBUserStatusRS>
    {
        public string LoginUser { get; set; }
        public bool LoginExists { get; set; }

        public DBUserStatusRS()
        {
            this.LoginUser = string.Empty;
            this.LoginExists = false;
        }

        public DBUserStatusRS(DBUserStatusRS rec)
        {
            this.LoginUser = rec.LoginUser;
            this.LoginExists = rec.LoginExists;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@LoginUser", LoginUser);
            parameters.AddWithValue("@LoginExists", LoginExists);
        }

        public DBUserStatusRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new DBUserStatusRS()
            {
                LoginUser = SqlReaderUtil.GetString(reader, nSeq++),
                LoginExists = SqlReaderUtil.GetInt32(reader, nSeq++) == 1 ? true : false
            };
            return rec;
        }

    }
}
