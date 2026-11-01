using System.Data;
using System.Text.Json;
using FIGSignalExSvc.Plugins;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FIGMarketLive;

public sealed record PluginCollectionRow(string Type,string DLLName,string Version,string Manifest,string Status,string Error);
public sealed record PluginCollectionSnapshot(string Root,IReadOnlyList<StudyPluginRegistration> Registrations,IReadOnlyList<PluginCollectionRow> Rows);

/// <summary>Manifest discovery uses the same managed loader as FIGSignalSvc. SQL is a catalog mirror.</summary>
public static class MarketLivePlugins
{
    private static readonly object gate=new();
    private static readonly Dictionary<string,(string Stamp,PluginCollectionSnapshot Snapshot)> cache=new(StringComparer.OrdinalIgnoreCase);
    public static PluginCollectionSnapshot Discover(string root)
    {
        root=Path.GetFullPath(root);
        lock(gate)
        {
            string stamp;
            try
            {
                stamp=Directory.Exists(root)?string.Join("|",Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories)
                    .Where(p=>Path.GetFileName(p).Equals("plugin.json",StringComparison.OrdinalIgnoreCase)||p.EndsWith(".dll",StringComparison.OrdinalIgnoreCase))
                    .Order(StringComparer.OrdinalIgnoreCase).Select(p=>{var f=new FileInfo(p);return $"{p}:{f.Length}:{f.LastWriteTimeUtc.Ticks}";})):"missing";
            }
            catch(Exception ex)when(ex is IOException or UnauthorizedAccessException)
            {return new(root,[],[new("","","",root,"Load error",ex.Message)]);}
            if(cache.TryGetValue(root,out var prior)&&prior.Stamp==stamp)return prior.Snapshot;
            var logger=new DiscoveryLogger();
            StudyPluginCatalog catalog;
            try{catalog=new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["StudyPlugins:Path"]=root}).Build(),logger);}
            catch(Exception ex){return new(root,[],[new("","","",root,"Load error",ex.Message)]);}
            var rows=catalog.Registrations.Select(r=>new PluginCollectionRow(r.ColType,Path.GetFileName(r.AssemblyPath),r.Version,r.AssemblyPath,"Loaded","")).ToList();
            if(rows.Count==0&&logger.Issues.Count==0)rows.Add(new("","","",root,"Load error","No plugin.json manifests were found in the plugin folder."));
            foreach(var issue in logger.Issues)
            {
                string type="",dll="";
                try
                {
                    using var doc=JsonDocument.Parse(File.ReadAllText(issue.Path));
                    foreach(var p in doc.RootElement.EnumerateObject())
                    {if(p.Name.Equals("colType",StringComparison.OrdinalIgnoreCase))type=p.Value.GetString()??"";if(p.Name.Equals("assembly",StringComparison.OrdinalIgnoreCase))dll=p.Value.GetString()??"";}
                }
                catch(Exception){} // Preserve the actual loader error for malformed/inaccessible manifests.
                rows.Add(new(type,dll,"",issue.Path,"Load error",issue.Message));
                if(issue.Message.Contains("already registered",StringComparison.OrdinalIgnoreCase))
                    for(int i=0;i<rows.Count;i++)if(rows[i].Type.Equals(type,StringComparison.OrdinalIgnoreCase))rows[i]=rows[i] with{Status="Load error",Error="Duplicate plugin manifests for this collection type. Remove the duplicate and refresh."};
            }
            for(int i=0;i<rows.Count;i++)if(rows[i].Status=="Loaded")
            {
                try{StudyConfigurationStore.ValidateType(new(rows[i].Type,rows[i].DLLName,""));}
                catch(Exception ex){rows[i]=rows[i] with{Status="Load error",Error=ex.Message};}
            }
            var available=rows.Where(r=>r.Status=="Loaded").Select(r=>r.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var snapshot=new PluginCollectionSnapshot(root,catalog.Registrations.Where(r=>available.Contains(r.ColType)).ToArray(),rows);
            cache[root]=(stamp,snapshot);return snapshot;
        }
    }
    public static async Task SyncAsync(string connectionString,PluginCollectionSnapshot snapshot,CancellationToken ct)
    {
        if(snapshot.Registrations.Count==0)return;
        await using var c=new SqlConnection(connectionString);await c.OpenAsync(ct);
        await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        foreach(var registration in snapshot.Registrations)
        {
            await using var cmd=new SqlCommand("""
                UPDATE dbo.ColType WITH(UPDLOCK,HOLDLOCK) SET DLLName=@Dll WHERE Type=@Type;
                IF @@ROWCOUNT=0 INSERT dbo.ColType(Type,DLLName,Description) VALUES(@Type,@Dll,'Discovered from plugin.json');
                """,c,tx);
            cmd.Parameters.AddWithValue("@Type",registration.ColType);cmd.Parameters.AddWithValue("@Dll",Path.GetFileName(registration.AssemblyPath));await cmd.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    private sealed class DiscoveryLogger:ILogger<StudyPluginCatalog>
    {
        public List<(string Path,string Message)> Issues {get;}=[];
        public IDisposable? BeginScope<TState>(TState state)where TState:notnull=>null;
        public bool IsEnabled(LogLevel logLevel)=>logLevel>=LogLevel.Warning;
        public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
        {
            if(!IsEnabled(level))return;
            var fields=state as IEnumerable<KeyValuePair<string,object?>>;
            string path=fields?.FirstOrDefault(p=>p.Key is "ManifestPath" or "PluginRoot").Value?.ToString()??"";
            Issues.Add((path,formatter(state,exception)+(exception is null?"":" — "+exception.Message)));
        }
    }
}
