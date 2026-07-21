using FIGCommon.DataAccess;
using FIGCommon.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace FIGCommon.Models
{
    public class UserRS : IDbEntity<UserRS>
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTimeOffset LockoutEnd { get; set; }
        public List<string> Roles { get; set; }

        public IdentityRole[] ToIdentityRoles()
        {
            var roles = new List<IdentityRole>();
            if (this.Roles != null)
            {
                foreach (string item in this.Roles)
                {
                    roles.Add(new IdentityRole(item));
                }
            }
            return roles.ToArray();
        }

        public UserRS()
        {
            this.Id = "";
            this.FirstName = "";
            this.LastName = "";
            this.UserName = "";
            this.Email = "";
            this.PhoneNumber = "";
            this.EmailConfirmed = false;
            this.PhoneNumberConfirmed = false;
            this.LockoutEnabled = false;
            this.AccessFailedCount = 0;
            this.LockoutEnd = new DateTimeOffset();
            this.Roles = new List<string>();
        }

        public UserRS(UserRS rec)
        {
            this.Id = rec.Id;
            this.FirstName = rec.FirstName;
            this.LastName = rec.LastName;
            this.UserName = rec.UserName;
            this.Email = rec.Email;
            this.PhoneNumber = rec.PhoneNumber;
            this.EmailConfirmed = rec.EmailConfirmed;
            this.PhoneNumberConfirmed = rec.PhoneNumberConfirmed;
            this.LockoutEnabled = rec.LockoutEnabled;
            this.AccessFailedCount = rec.AccessFailedCount;
            this.LockoutEnd = rec.LockoutEnd;
            this.Roles = new List<string>();
            this.Roles.AddRange(rec.Roles);
        }

        public AspNetUser ToIdentityUser()
        {
            var identitiyUser = new AspNetUser();
            if (this.Id != null && this.Id.Length > 0)
            {
                identitiyUser.Id = this.Id;
            }
            identitiyUser.UserName = this.UserName;
            identitiyUser.FirstName = this.FirstName;
            identitiyUser.LastName = this.LastName;
            identitiyUser.Email = this.Email;
            identitiyUser.PhoneNumber = this.PhoneNumber;
            identitiyUser.EmailConfirmed = this.EmailConfirmed;
            identitiyUser.PhoneNumberConfirmed = this.PhoneNumberConfirmed;
            identitiyUser.LockoutEnabled = this.LockoutEnabled;
            identitiyUser.LockoutEnd = this.LockoutEnd;
            identitiyUser.AccessFailedCount = this.AccessFailedCount;
            return identitiyUser;
        }
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
            parameters.AddWithValue("@AccessFailedCount", AccessFailedCount);
            parameters.AddWithValue("@LockoutEnabled", LockoutEnabled);
            parameters.AddWithValue("@LockoutEnd", LockoutEnd);
        }

        public UserRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var user = new UserRS()
            {
                Id = SqlReaderUtil.GetString(reader, nSeq++),
                UserName = SqlReaderUtil.GetString(reader, nSeq++),
                Email = SqlReaderUtil.GetString(reader, nSeq++),
                EmailConfirmed = SqlReaderUtil.GetBoolean(reader, nSeq++),
                PhoneNumber = SqlReaderUtil.GetString(reader, nSeq++),
                PhoneNumberConfirmed = SqlReaderUtil.GetBoolean(reader, nSeq++),
                LockoutEnd = SqlReaderUtil.GetDateTimeOffset(reader, nSeq++),
                LockoutEnabled = SqlReaderUtil.GetBoolean(reader, nSeq++),
                AccessFailedCount = SqlReaderUtil.GetInt32(reader, nSeq++),
                FirstName = SqlReaderUtil.GetString(reader, nSeq++),
                LastName = SqlReaderUtil.GetString(reader, nSeq++),
            };
            string roles = SqlReaderUtil.GetString(reader, nSeq++);
            user.Roles = roles.Split(',').ToList();
            return user;
        }
    }
}
