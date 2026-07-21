using FIGCommon.Exceptions;
using FIGCommon.Utilities;
using Microsoft.Data.SqlClient;
using System.Data;


namespace FIGCommon.DataAccess
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0063:Use simple 'using' statement", Justification = "<Pending>")]
    public class RepositoryBase
    {
        private ILogger? _logger = null;
        private string _connectionString = "";

        public RepositoryBase()
        {

        }

        public RepositoryBase(ILogger? logger)
        {
            this._logger = logger;
        }

        public RepositoryBase(string dbConnectionString, ILogger? logger)
        {
            this._logger = logger;
            _connectionString = dbConnectionString;
        }

        public void Initialize(string dbConnectionString, ILogger? logger)
        {
            this._logger = logger;
            string connString = dbConnectionString;
            if (connString.ToLower().Contains("data source="))
            {
                _connectionString = connString;
            }
            else
            {
                // decrypt connection string
                _connectionString = ProtectedDataUtil.Unprotect(connString) ?? "";
            }
            if (dbConnectionString != "" && _connectionString == "")
            {
                _logger?.LogError("Database DefaultConnection Configuration is invalid");
                throw new SqlConfigException();
            }
        }

        #region Properties
        public string ConnectionString
        {
            get
            {
                return _connectionString;
            }
            set
            {
                _connectionString = value;
            }
        }

        public string DatabaseName
        {
            get
            {
                string connStr = ConnectionString;
                string[] parts = connStr.Split(";");
                string dbName = "";
                foreach (var part in parts)
                {
                    if (part.Contains("Initial Catalog"))
                    {
                        dbName = part.Split("=")[1];
                        break;
                    }
                }
                return dbName;
            }
        }

        #endregion Properties

        public void ConnectionClearPool()
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                SqlConnection.ClearPool(connection);
            }
        }

        public static void ConnectionClearAllPools()
        {
            SqlConnection.ClearAllPools();
        }

        #region Logs
        public void LogError(string? message, params object?[] args)
        {
            _logger?.LogError(message, args);
        }
        public void LogDebug(string? message, params object?[] args)
        {
            _logger?.LogDebug(message, args);
        }
        #endregion Logs

        #region Select
        public T? Select<T>(T prototype, string storedProc) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Select(connection, prototype, storedProc);
            }
        }

        public static T? Select<T>(SqlConnection connection, T prototype, string storedProc) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = storedProc.StartsWith("usp_") ? CommandType.StoredProcedure : CommandType.Text;
                command.CommandText = storedProc;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return prototype.CreateFromSqlDataReader(reader);
                    }
                }
            }

            return default;
        }

        public T? Select<T, P>(T prototype, string storedProc, string? paramName = null, P? param = default) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Select(connection, prototype, storedProc, paramName, param);
            }
        }

        public static T? Select<T, P>(SqlConnection connection, T prototype, string storedProc, string? paramName = null, P? param = default, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;

                if (null != paramName)
                {
                    command.Parameters.AddWithValue(paramName, param);
                }

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return prototype.CreateFromSqlDataReader(reader);
                    }
                }
            }

            return default;
        }

        public T? Select<T, P1, P2>(T prototype, string storedProc, string paramName1, P1 param1, string paramName2, P2 param2) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Select<T, P1, P2>(connection, prototype, storedProc, paramName1, param1, paramName2, param2);
            }
        }

        public static T? Select<T, P1, P2>(SqlConnection connection, T prototype, string storedProc, string paramName1, P1 param1, string paramName2, P2 param2, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;
                command.Parameters.AddWithValue(paramName1, param1);
                command.Parameters.AddWithValue(paramName2, param2);
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return prototype.CreateFromSqlDataReader(reader);
                    }
                }
            }

            return default;
        }

        public T? Select<T, P1, P2, P3>(T prototype, string storedProc, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Select<T, P1, P2, P3>(connection, prototype, storedProc, paramName1, param1, paramName2, param2, paramName3, param3);
            }
        }

        public static T? Select<T, P1, P2, P3>(SqlConnection connection, T prototype, string storedProc, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, SqlTransaction? tx = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;
                command.Parameters.AddWithValue(paramName1, param1);
                command.Parameters.AddWithValue(paramName2, param2);
                command.Parameters.AddWithValue(paramName3, param3);
                if (null != tx)
                {
                    command.Transaction = tx;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return prototype.CreateFromSqlDataReader(reader);
                    }
                }
            }

            return default;
        }

        public T? Select<T>(T prototype, string storedProc, Dictionary<string, object> parameters) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Select<T>(connection, prototype, storedProc, parameters);
            }
        }

        public static T? Select<T>(SqlConnection connection, T prototype, string storedProc, Dictionary<string, object> parameters) where T : IDbEntity<T>
        {
            using (SqlCommand command = new SqlCommand(storedProc, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? prototype.CreateFromSqlDataReader(reader) : default;
                }
            }
        }



        public List<T> SelectMulti<T>(T prototype, string procName) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return SelectMulti(connection, prototype, procName, null, 0);
            }
        }

        public List<T> SelectMulti<T, P>(T prototype, string procName, string? paramName, P? param) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return SelectMulti(connection, prototype, procName, paramName, param);
            }
        }

        public static List<T> SelectMulti<T, P>(SqlConnection connection, T prototype, string procName, string? paramName, P? param, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            List<T> entities = new();

            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = procName.StartsWith("usp_") ? CommandType.StoredProcedure : CommandType.Text;
                command.CommandText = procName;
                if (null != paramName)
                {
                    command.Parameters.AddWithValue(paramName, param);
                }
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(prototype.CreateFromSqlDataReader(reader));
                    }
                }
            }

            return entities;
        }

        public List<T> SelectMulti<T, P1, P2>(T prototype, string procName, string paramName1, P1 param1, string paramName2, P2 param2) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return SelectMulti(connection, prototype, procName, paramName1, param1, paramName2, param2);
            }
        }

        public static List<T> SelectMulti<T, P1, P2>(SqlConnection connection, T prototype, string procName, string paramName1, P1 param1, string paramName2, P2 param2, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            List<T> entities = new();

            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                command.Parameters.AddWithValue(paramName1, param1);
                command.Parameters.AddWithValue(paramName2, param2);
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(prototype.CreateFromSqlDataReader(reader));
                    }
                }
            }

            return entities;
        }

        public List<T> SelectMulti<T, P1, P2, P3>(T prototype, string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return SelectMulti(connection, prototype, procName, paramName1, param1, paramName2, param2, paramName3, param3);
            }
        }

        public static List<T> SelectMulti<T, P1, P2, P3>(SqlConnection connection, T prototype, string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            List<T> entities = new();

            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                command.Parameters.AddWithValue(paramName1, param1);
                command.Parameters.AddWithValue(paramName2, param2);
                command.Parameters.AddWithValue(paramName3, param3);
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(prototype.CreateFromSqlDataReader(reader));
                    }
                }
            }

            return entities;
        }

        public List<T> SelectMulti<T>(T prototype, string storedProc, Dictionary<string, object> parameters) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return SelectMulti<T>(connection, prototype, storedProc, parameters);
            }
        }

        public static List<T> SelectMulti<T>(SqlConnection connection, T prototype, string storedProc, Dictionary<string, object> parameters, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            List<T> entities = new();

            using (SqlCommand command = new SqlCommand(storedProc, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                foreach (var param in parameters)
                {
                    command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entities.Add(prototype.CreateFromSqlDataReader(reader));
                    }
                }
            }
            return entities;
        }


        #endregion Select

        #region Insert
        public int Insert<T>(T entity, string storedProc) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Insert(entity, connection, storedProc);
            }
        }

        public static int Insert<T>(T entity, SqlConnection connection, string storedProc, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                entity.ToSqlCommandParameters(command.Parameters);

                // expecting collation set to Latin1_General_CI_AS
                return command.ExecuteNonQuery();
            }
        }

        public int Insert<T, P>(T entity, string storedProc, string paramName, P param) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Insert(entity, connection, storedProc, paramName, param);
            }
        }

        public static int Insert<T, P>(T entity, SqlConnection connection, string storedProc, string paramName, P param, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                command.Parameters.AddWithValue(paramName, param);
                entity.ToSqlCommandParameters(command.Parameters);

                // expecting collation set to Latin1_General_CI_AS
                return command.ExecuteNonQuery();
            }
        }

        //public int Insert<T>(T entity, string procName, string idOutputParamName) where T : IDbEntity<T>
        //{
        //    using (SqlConnection connection = new(ConnectionString))
        //    {
        //        connection.Open();
        //        return Insert<T>(entity, connection, procName, idOutputParamName);
        //    }
        //}

        //public int Insert<T>(T entity, SqlConnection connection, string storedProc, string idOutputParamName, SqlTransaction transaction = null) where T : IDbEntity<T>
        //{
        //    using (SqlCommand command = new SqlCommand())
        //    {
        //        command.Connection = connection;
        //        command.CommandType = CommandType.StoredProcedure;
        //        command.CommandText = storedProc;
        //        if (null != transaction)
        //        {
        //            command.Transaction = transaction;
        //        }

        //        SqlParameter idOutputParam = new SqlParameter(idOutputParamName, SqlDbType.Int)
        //        {
        //            Direction = ParameterDirection.Output
        //        };
        //        command.Parameters.Add(idOutputParam);
        //        entity.ToSqlCommandParameters(command.Parameters);

        //        // expecting collation set to Latin1_General_CI_AS
        //        int result = command.ExecuteNonQuery();
        //        if (result < 0)
        //        {
        //            throw (new SqlInsertException("Failed to insert record into database. Unknown error"));
        //        }

        //        return (int)idOutputParam.Value;
        //    }
        //}

        public long Insert<T>(T entity, string procName, string idOutputParamName) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Insert(entity, connection, procName, idOutputParamName);
            }
        }

        public static long Insert<T>(T entity, SqlConnection connection, string storedProc, string idOutputParamName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storedProc;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                SqlParameter idOutputParam = new(idOutputParamName, SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(idOutputParam);
                entity.ToSqlCommandParameters(command.Parameters);

                // expecting collation set to Latin1_General_CI_AS
                int result = command.ExecuteNonQuery();
                if (result < 0)
                {
                    throw (new SqlInsertException("Failed to insert record into database. Unknown error"));
                }

                return (long)idOutputParam.Value;
            }
        }
        #endregion Insert

        #region Upsert
        private static SqlParameter CreateOutputParam<R>(string name)
        {
            if (typeof(R) == typeof(string))
            {
                return new SqlParameter(name, SqlDbType.VarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };
            }

            if (typeof(R) == typeof(long))
            {
                return new SqlParameter(name, SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
            }

            if (typeof(R) == typeof(int))
            {
                return new SqlParameter(name, SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
            }

            throw new NotSupportedException($"Unsupported output id type: {typeof(R).FullName}");
        }

        public long Upsert<T>(T entity, string procName, string idOutputParamName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Upsert<T, int>(connection, entity, procName, idOutputParamName, transaction);
            }
        }

        public bool Upsert<T>(T entity, string procName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Upsert(connection, entity, procName, transaction);
            }
        }

        public static R Upsert<T, R>(SqlConnection connection, T entity, string procName, string idOutputParamName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                var idOutputParam = CreateOutputParam<R>(idOutputParamName);
                command.Parameters.Add(idOutputParam);
                entity.ToSqlCommandParameters(command.Parameters);

                command.ExecuteNonQuery();
                return (R)idOutputParam.Value;
            }
        }


        public SqlTransaction? OpenTransaction()
        {
            SqlConnection? connection = null;
            SqlTransaction? trans = null;
            try
            {
                connection = new(ConnectionString);
                connection.Open();
                trans = connection.BeginTransaction();
                return trans;
            }
            catch (Exception ex)
            {
                // Common exceptions:
                // - Invalid connection string (FormatException)
                // - Network/server unavailable (SqlException)
                // - Authentication failures (SqlException)
                // - Database doesn't exist (SqlException)
                LogError($"OpenConnection Error - {ex.Message}");
                trans?.Dispose();
                connection?.Dispose();
                return null;
            }
        }

        public static bool Upsert<T>(SqlConnection connection, T entity, string procName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                entity.ToSqlCommandParameters(command.Parameters);
                command.ExecuteNonQuery();
                return true;
            }
        }

        #endregion Upsert

        #region Update
        public int Update<T, P>(T entity, string procName, string? paramName = null, P? param = default) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Update(connection, entity, procName, paramName, param);
            }
        }

        public static int Update<T, P>(SqlConnection connection, T entity, string procName, string? paramName = null, P? param = default, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                if (null != paramName)
                {
                    command.Parameters.AddWithValue(paramName, param);
                }
                SqlParameter returnParameter = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnParameter);

                entity.ToSqlCommandParameters(command.Parameters);

                int result = command.ExecuteNonQuery();

                return (int)returnParameter.Value;

                //if (1 != result)
                //{
                //    throw (new SqlUpdateException("Failed to update record in database. Unknown error"));
                //}
            }
        }

        public int Update<T>(T entity, string procName) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Update(connection, entity, procName);
            }
        }

        public static int Update<T>(SqlConnection connection, T entity, string procName, SqlTransaction? transaction = null) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                SqlParameter returnParameter = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnParameter);

                entity.ToSqlCommandParameters(command.Parameters);

                int result = command.ExecuteNonQuery();  // result are the number of rows affected

                return (int)returnParameter.Value;

                //if (1 != result)
                //{
                //    throw (new SqlUpdateException("Failed to update record in database. Unknown error"));
                //}
            }
        }

        public int Update<P>(string procName, string paramName, P param)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Update(connection, procName, paramName, param, null, 0, null, 0);
            }
        }

        public int Update<P1, P2, P3>(string procName, string paramName1, P1 param1, string? paramName2, P2? param2, string? paramName3 = null, P3? param3 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Update(connection, procName, paramName1, param1, paramName2, param2, paramName3, param3);
            }
        }

        public static int Update<P1, P2, P3>(SqlConnection connection, string procName, string paramName1, P1 param1, string? paramName2, P2? param2, string? paramName3, P3? param3, SqlTransaction? transaction = null)
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                command.Parameters.AddWithValue(paramName1, param1);
                if (null != paramName2)
                {
                    command.Parameters.AddWithValue(paramName2, param2);
                }
                if (null != paramName3)
                {
                    command.Parameters.AddWithValue(paramName3, param3);
                }
                SqlParameter returnParameter = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnParameter);

                int result = command.ExecuteNonQuery();
                return (int)returnParameter.Value;
                //if (1 != result)
                //{
                //    throw (new SqlProcException($"Failed to execute '{procName}'. Unknown error"));
                //}
            }
        }
        #endregion Update

        #region Delete
        public int Delete<P1>(string procName, string paramName1, P1 param1)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;

                    command.Parameters.AddWithValue(paramName1, param1);
                    return command.ExecuteNonQuery();
                }
            }
        }

        public int Delete<P1, P2>(string procName, string paramName1, P1 param1, string paramName2, P1 param2)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;

                    command.Parameters.AddWithValue(paramName1, param1);
                    command.Parameters.AddWithValue(paramName2, param2);
                    return command.ExecuteNonQuery();
                }
            }
        }

        public int Delete<P1, P2, P3>(string procName, string paramName1, P1 param1, string? paramName2 = null, P2? param2 = default, string? paramName3 = null, P3? param3 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Delete(connection, procName, paramName1, param1, paramName2, param2, paramName3, param3);
            }
        }

        public static int Delete<P1>(SqlConnection connection, SqlTransaction? transaction, string procName, string paramName1, P1 param1)
        {
            return Delete(connection, procName, paramName1, param1, null, 0, null, 0, transaction);
        }

        public static int Delete<P1, P2>(SqlConnection connection, SqlTransaction? transaction, string procName, string paramName1, P1 param1, string paramName2, P2 param2)
        {
            return Delete(connection, procName, paramName1, param1, paramName2, param2, null, 0, transaction);
        }

        public static int Delete<P1, P2, P3>(SqlConnection connection, string procName, string paramName1, P1 param1, string? paramName2, P2? param2, string? paramName3, P3? param3
            , SqlTransaction? transaction = null)
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;

                command.Parameters.AddWithValue(paramName1, param1);
                if (null != paramName2)
                {
                    command.Parameters.AddWithValue(paramName2, param2);
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }
                }
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                return command.ExecuteNonQuery();
            }
        }

        public int Delete<T>(T entity, string procName) where T : IDbEntity<T>
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return Delete(entity, connection, null, procName);
            }
        }

        public static int Delete<T>(T entity, SqlConnection connection, SqlTransaction? transaction, string procName) where T : IDbEntity<T>
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }
                entity.ToSqlCommandParameters(command.Parameters);
                return command.ExecuteNonQuery();
            }
        }
        #endregion Delete

        #region ExecuteText
        public void ExecutScript(string scriptText)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.Text;
                    command.CommandText = scriptText;
                    command.ExecuteNonQuery();
                }
            }
        }
        #endregion ExecuteText


        #region ExecuteScalar
        public int ExecuteScalar(string procName)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }
        public int ExecuteScalar<P1>(string procName, string paramName1, P1 param1)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return ExecuteScalar<P1>(connection, procName, paramName1, param1, null);
            }
        }
        public static int ExecuteScalar<P1>(SqlConnection connection, string procName, string paramName1, P1 param1, SqlTransaction? transaction = null)
        {
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);
                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }

        public int ExecuteScalar<P1, P2>(string procName, string paramName1, P1 param1, string? paramName2 = null, P2? param2 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                return ExecuteScalar<P1, P2>(connection, procName, paramName1, param1, paramName2, param2, null);
            }
        }
        public static int ExecuteScalar<P1, P2>(SqlConnection connection, string procName, string paramName1, P1 param1, string? paramName2 = null, P2? param2 = default, SqlTransaction? transaction = null)
        {
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
            }
        }
        public static int ExecuteScalar<P1, P2, P3, P4>(SqlConnection connection, string procName, string paramName1, P1 param1, string? paramName2, P2 param2, string? paramName3, P3 param3, string? paramName4, P4 param4, SqlTransaction? transaction = null)
        {
            using (SqlCommand command = new())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = procName;
                command.Parameters.AddWithValue(paramName1, param1);
                if (null != paramName2)
                {
                    command.Parameters.AddWithValue(paramName2, param2);
                }
                if (null != paramName3)
                {
                    command.Parameters.AddWithValue(paramName3, param3);
                }
                if (null != paramName4)
                {
                    command.Parameters.AddWithValue(paramName4, param4);
                }

                SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(returnValue);

                if (null != transaction)
                {
                    command.Transaction = transaction;
                }

                command.ExecuteScalar();

                return (int)returnValue.Value;
            }
        }
        public int ExecuteScalar<P1, P2, P3>(string procName, string paramName1, P1 param1, string? paramName2 = null, P2? param2 = default, string? paramName3 = null, P3? param3 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }
        public int ExecuteScalar<P1, P2, P3, P4>(string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, string? paramName4 = null, P4? param4 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }
                    if (null != paramName4)
                    {
                        command.Parameters.AddWithValue(paramName4, param4);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }
        public int ExecuteScalar<P1, P2, P3, P4, P5>(string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, string? paramName4, P4? param4, string? paramName5 = null, P5? param5 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }
                    if (null != paramName4)
                    {
                        command.Parameters.AddWithValue(paramName4, param4);
                    }
                    if (null != paramName5)
                    {
                        command.Parameters.AddWithValue(paramName5, param5);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }
        public int ExecuteScalar<P1, P2, P3, P4, P5, P6>(string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, string? paramName4, P4 param4, string? paramName5, P5? param5, string? paramName6 = null, P6? param6 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }
                    if (null != paramName4)
                    {
                        command.Parameters.AddWithValue(paramName4, param4);
                    }
                    if (null != paramName5)
                    {
                        command.Parameters.AddWithValue(paramName5, param5);
                    }
                    if (null != paramName6)
                    {
                        command.Parameters.AddWithValue(paramName6, param6);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }

        public int ExecuteScalar<P1, P2, P3, P4, P5, P6, P7>(string procName, string paramName1, P1 param1, string paramName2, P2 param2, string paramName3, P3 param3, 
            string? paramName4, P4 param4, string? paramName5, P5? param5, string? paramName6, P6? param6, string? paramName7 = null, P7? param7 = default)
        {
            using (SqlConnection connection = new(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new())
                {
                    command.Connection = connection;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = procName;
                    command.Parameters.AddWithValue(paramName1, param1);
                    if (null != paramName2)
                    {
                        command.Parameters.AddWithValue(paramName2, param2);
                    }
                    if (null != paramName3)
                    {
                        command.Parameters.AddWithValue(paramName3, param3);
                    }
                    if (null != paramName4)
                    {
                        command.Parameters.AddWithValue(paramName4, param4);
                    }
                    if (null != paramName5)
                    {
                        command.Parameters.AddWithValue(paramName5, param5);
                    }
                    if (null != paramName6)
                    {
                        command.Parameters.AddWithValue(paramName6, param6);
                    }
                    if (null != paramName7)
                    {
                        command.Parameters.AddWithValue(paramName7, param7);
                    }

                    SqlParameter returnValue = new("@ReturnValue", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    command.Parameters.Add(returnValue);

                    command.ExecuteScalar();

                    return (int)returnValue.Value;
                }
            }
        }
        #endregion ExecuteScalar

        #region misc
        public void WriteTable(DataTable table)
        {
            using (SqlBulkCopy bulkCopy = new(ConnectionString, SqlBulkCopyOptions.FireTriggers))
            {
                bulkCopy.BulkCopyTimeout = 600; // in seconds
                bulkCopy.DestinationTableName = table.TableName;
                bulkCopy.WriteToServer(table);
            }
        }
        public int ExecuteProc<P1, P2>(string procName, string paramName1, P1 param1, string? paramName2 = null, P2? param2 = default)
        {
            return ExecuteScalar<P1, P2>(procName, paramName1, param1, paramName2, param2);
        }
        public int ExecuteProc<P1>(string procName, string paramName1, P1 param1)
        {
            return ExecuteScalar<P1>(procName, paramName1, param1);
        }


        #endregion misc
    }
}

