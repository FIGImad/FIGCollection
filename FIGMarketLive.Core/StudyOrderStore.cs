using System.Data;
using Microsoft.Data.SqlClient;

namespace FIGMarketLive;

public sealed class StudyOrderStore(string connectionString)
{
    public async Task MoveAsync(int id,int direction,IReadOnlyList<StudyConfigurationRow> expected,CancellationToken ct)
    {
        if(direction is not (-1 or 1))throw new ArgumentOutOfRangeException(nameof(direction));
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var current=new List<(int Id,int Order)>();
        await using(var read=new SqlCommand("SELECT Id,OrderSeq FROM dbo.StudyCol WITH(UPDLOCK,HOLDLOCK) ORDER BY Id",c,tx))
        await using(var reader=await read.ExecuteReaderAsync(ct))
            while(await reader.ReadAsync(ct))current.Add((reader.GetInt32(0),reader.GetInt32(1)));
        current=current.OrderBy(s=>s.Order).ThenBy(s=>s.Id).ToList();
        if(!current.SequenceEqual(expected.OrderBy(s=>s.OrderSeq).ThenBy(s=>s.Id).Select(s=>(s.Id,s.OrderSeq))))
            throw new InvalidDataException("Study collection order changed. Reload Study Collections before moving a row.");
        int index=current.FindIndex(s=>s.Id==id),target=index+direction;
        if(index<0)throw new InvalidDataException("Select an existing StudyCol.");
        if(target<0||target>=current.Count){await tx.CommitAsync(ct);return;}
        (current[index],current[target])=(current[target],current[index]);
        await using(var create=new SqlCommand("CREATE TABLE #StudyOrder(Id int PRIMARY KEY,OrderSeq int NOT NULL)",c,tx))await create.ExecuteNonQueryAsync(ct);
        using var data=new DataTable();data.Columns.Add("Id",typeof(int));data.Columns.Add("OrderSeq",typeof(int));
        for(int n=0;n<current.Count;n++)data.Rows.Add(current[n].Id,n+1);
        using(var copy=new SqlBulkCopy(c,SqlBulkCopyOptions.Default,tx){DestinationTableName="#StudyOrder"})
        {copy.ColumnMappings.Add("Id","Id");copy.ColumnMappings.Add("OrderSeq","OrderSeq");await copy.WriteToServerAsync(data,ct);}
        await using(var update=new SqlCommand("UPDATE s SET OrderSeq=o.OrderSeq FROM dbo.StudyCol s JOIN #StudyOrder o ON o.Id=s.Id WHERE s.OrderSeq<>o.OrderSeq",c,tx))await update.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
    }
}
