using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using FIGCommon.DataAccess;
using FIGCommon.Utilities;

namespace FIGCommon.Models
{
    public class ValidationTokenRS : IDbEntity<ValidationTokenRS>
    {
        public long ValidationTokenId { get; set; }
        public string Token { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }

        public ValidationTokenRS()
        {
            this.ValidationTokenId = -1;
            this.Token = "";
            this.UserId = "";
            this.Timestamp = new DateTime();
        }
        public ValidationTokenRS(ValidationTokenRS rec)
        {
            this.ValidationTokenId = rec.ValidationTokenId;
            this.Token = rec.Token;
            this.UserId = rec.UserId;
            this.Timestamp = rec.Timestamp;
        }

        // IDbEntity implementation
        public void ToSqlCommandParameters(SqlParameterCollection parameters)
        {
            parameters.AddWithValue("@ValidationTokenId", ValidationTokenId);
            parameters.AddWithValue("@Token", Token);
            parameters.AddWithValue("@UserId", UserId);
        }

        public ValidationTokenRS CreateFromSqlDataReader(SqlDataReader reader)
        {
            int nSeq = 0;
            var rec = new ValidationTokenRS()
            {
                ValidationTokenId = SqlReaderUtil.GetInt64(reader, nSeq++),
                Token = SqlReaderUtil.GetString(reader, nSeq++),
                UserId = SqlReaderUtil.GetString(reader, nSeq++),
                Timestamp = SqlReaderUtil.GetDateTime(reader, nSeq++)
            };
            return rec;
        }

        public static long GenerateValidationTokenId()
        {
            //return new Random((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds).Next();
            using (var generator = RandomNumberGenerator.Create())
            {
                var bytes = new byte[4];
                generator.GetBytes(bytes);
                return BitConverter.ToUInt32(bytes, 0);
            }
        }
    }
}
