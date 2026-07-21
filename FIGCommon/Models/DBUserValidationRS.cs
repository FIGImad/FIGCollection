using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public partial class DBUserValidationRS : IDbEntity<DBUserValidationRS>
    {
        public string DatabaseName { get; set; }
        public string LoginName { get; set; }
        public bool DBExists { get; set; }
        public bool UserReg { get; set; }
        public bool MappedToLoginSID { get; set; }
        public bool IsOwner { get; set; }
        public bool IsPublic { get; set; }
        public string Message { get; set; }

        public DBUserValidationRS()
        {
            this.DatabaseName = string.Empty;
            this.LoginName = string.Empty;
            this.DBExists = false;
            this.UserReg = false;
            this.MappedToLoginSID = false;
            this.IsOwner = false;
            this.IsPublic = false;
            this.Message = string.Empty;
        }

        public DBUserValidationRS(DBUserValidationRS rec)
        {
            this.DatabaseName = rec.DatabaseName;
            this.LoginName = rec.LoginName;
            this.DBExists = rec.DBExists;
            this.UserReg = rec.UserReg;
            this.MappedToLoginSID = rec.MappedToLoginSID;
            this.IsOwner = rec.IsOwner;
            this.IsPublic = rec.IsPublic;
            this.Message = rec.Message;
        }

        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
        }

        public DBUserValidationRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new DBUserValidationRS()
            {
                DatabaseName = SqlReaderUtil.GetString(reader, nSeq++),
                LoginName = SqlReaderUtil.GetString(reader, nSeq++),
                DBExists = SqlReaderUtil.GetBoolean(reader, nSeq++),
                UserReg = SqlReaderUtil.GetBoolean(reader, nSeq++),
                MappedToLoginSID = SqlReaderUtil.GetBoolean(reader, nSeq++),
                IsOwner = SqlReaderUtil.GetBoolean(reader, nSeq++),
                IsPublic = SqlReaderUtil.GetBoolean(reader, nSeq++),
                Message = string.Empty
            };
            return rec;
        }

    }
}
