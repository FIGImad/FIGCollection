using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Identity;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class AspNetUser : IdentityUser, IDbEntity<AspNetUser>
    {
        [ProtectedPersonalData]
        public string FirstName { get; set; } = "";
        [ProtectedPersonalData]
        public string LastName { get; set; } = "";

        public AspNetUser() : base() { }
        public AspNetUser(string userName) : base(userName) { }

        // IDbEntity implementation
        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@Id", Id);
            parameters.AddWithValue("@FirstName", FirstName);
            parameters.AddWithValue("@LastName", LastName);
            parameters.AddWithValue("@PhoneNumber", PhoneNumber);
            parameters.AddWithValue("@PhoneNumberConfirmed", PhoneNumberConfirmed);
            parameters.AddWithValue("@UserName", UserName);
            parameters.AddWithValue("@Email", Email);
            parameters.AddWithValue("@EmailConfirmed", EmailConfirmed);
            parameters.AddWithValue("@PasswordHash", PasswordHash);
            //     A random value that must change whenever a users credentials change (password
            //     changed, login removed)
            parameters.AddWithValue("@SecurityStamp", SecurityStamp);
            //     A random value that must change whenever a user is persisted to the store
            parameters.AddWithValue("@ConcurrencyStamp", ConcurrencyStamp);
            parameters.AddWithValue("@AccessFailedCount", AccessFailedCount);
            parameters.AddWithValue("@LockoutEnabled", LockoutEnabled);
            parameters.AddWithValue("@LockoutEnd", LockoutEnd);
        }

        public AspNetUser CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var user = new AspNetUser()
            {
                Id = SqlReaderUtil.GetString(reader, nSeq++),
                UserName = SqlReaderUtil.GetString(reader, nSeq++),
                NormalizedUserName = SqlReaderUtil.GetString(reader, nSeq++),
                Email = SqlReaderUtil.GetString(reader, nSeq++),
                NormalizedEmail = SqlReaderUtil.GetString(reader, nSeq++),
                EmailConfirmed = SqlReaderUtil.GetBoolean(reader, nSeq++),
                PasswordHash = SqlReaderUtil.GetString(reader, nSeq++),
                SecurityStamp = SqlReaderUtil.GetString(reader, nSeq++),
                ConcurrencyStamp = SqlReaderUtil.GetString(reader, nSeq++),
                PhoneNumber = SqlReaderUtil.GetString(reader, nSeq++),
                PhoneNumberConfirmed = SqlReaderUtil.GetBoolean(reader, nSeq++),
                LockoutEnd = SqlReaderUtil.GetDateTimeOffset(reader, nSeq++),
                LockoutEnabled = SqlReaderUtil.GetBoolean(reader, nSeq++),
                AccessFailedCount = SqlReaderUtil.GetInt32(reader, nSeq++),
                FirstName = SqlReaderUtil.GetString(reader, nSeq++),
                LastName = SqlReaderUtil.GetString(reader, nSeq++),
            };
            return user;
        }
    }
}
