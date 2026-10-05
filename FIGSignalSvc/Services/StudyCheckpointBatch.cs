using System.Text.Json;
using FIG.Studies;
using Microsoft.Data.SqlClient;

namespace FIGSignalExSvc.Services;

public sealed record StudyCheckpointPayload(long RawTime, string Json)
{
    // SQL nvarchar and .NET strings both use UTF-16; bound batches by payload size.
    public long Bytes => (long)Json.Length * sizeof(char);
    public static StudyCheckpointPayload Capture(CollectionCheckpoint checkpoint)
        => new(checkpoint.RawTime, JsonSerializer.Serialize(checkpoint, StudyStateJson.Options));
}

/// <summary>A bounded batch of serialized checkpoints; no replay-sized temporary file.</summary>
public sealed class StudyCheckpointBatch : IDisposable
{
    private readonly List<StudyCheckpointPayload> items = [];
    public IReadOnlyList<StudyCheckpointPayload> Items => items;
    public int Count => items.Count;
    public long PayloadBytes { get; private set; }
    public void Add(CollectionCheckpoint checkpoint) => Add(StudyCheckpointPayload.Capture(checkpoint));
    public void Add(StudyCheckpointPayload payload)
    {
        items.Add(payload);
        PayloadBytes += payload.Bytes;
    }
    public void Clear() { items.Clear(); PayloadBytes = 0; }
    public void Dispose() => Clear();

    public void Save(StudyCheckpointStore store, int studyColId, IReadOnlySet<long> writtenTimes, SqlTransaction transaction)
    {
        if (items.Any(p => !writtenTimes.Contains(p.RawTime)) || items.Select(p => p.RawTime).Distinct().Count() != items.Count)
            throw new InvalidDataException("Checkpoint batch contains duplicate or unwritten bars.");
        store.SaveBatch(studyColId, items, transaction);
    }
}
