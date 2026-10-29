using System.Data;
using System.Globalization;
using FIGCommon.DataAccess;
using Microsoft.Data.SqlClient;
namespace FIGSignalBacktest;

internal static class Database
{
    public static string ConnectionString(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
            throw new ArgumentException("--connection-string is required for database input.");
        try
        {
            var repository = new RepositoryBase();
            repository.Initialize(configured, null);
            return repository.ConnectionString;
        }
        catch { throw new InvalidOperationException("Cannot resolve DefaultConnection; check --connection-string and FIG_MASTER_KEY."); }
    }

    public static List<Price> Read(Options o, string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {(o.MaxBars > 0 ? "TOP (@MaxBars) " : "")}Id,DataSetId,RawTime,[Open],High,Low,[Close],Volume FROM dbo.PriceData WHERE DataSetId=@DataSetId ORDER BY RawTime ASC";
            command.Parameters.Add("@DataSetId", SqlDbType.Int).Value = o.Dataset;
            if (o.MaxBars > 0) command.Parameters.Add("@MaxBars", SqlDbType.Int).Value = o.MaxBars;
            using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
            var result = new List<Price>();
            while (reader.Read())
                result.Add(new(Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
                    Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
                    Convert.ToInt64(reader.GetValue(2), CultureInfo.InvariantCulture),
                    Convert.ToDouble(reader.GetValue(3), CultureInfo.InvariantCulture),
                    Convert.ToDouble(reader.GetValue(4), CultureInfo.InvariantCulture),
                    Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture),
                    Convert.ToDouble(reader.GetValue(6), CultureInfo.InvariantCulture),
                    Convert.ToInt64(reader.GetValue(7), CultureInfo.InvariantCulture)));
            return result;
        }
        catch (SqlException ex) { throw new IOException($"SQL Server price read failed (error {ex.Number}). Check DefaultConnection, database access and network connectivity."); }
        catch (ArgumentException) { throw new IOException("Invalid SQL Server DefaultConnection configuration."); }
    }
}
