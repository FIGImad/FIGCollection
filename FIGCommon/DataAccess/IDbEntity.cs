using Microsoft.Data.SqlClient;


namespace FIGCommon.DataAccess
{
    public interface IDbEntity<T>
    {
        void ToSqlCommandParameters(SqlParameterCollection parameters);
        T CreateFromSqlDataReader(SqlDataReader reader);
    }
}
