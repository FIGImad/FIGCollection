using System.Text.Json;
using FIGCommon.Models;
using FIGSignalExSvc.Services;

int checks = 0;
void Check(bool value, string description)
{
    if (!value) throw new Exception(description);
    checks++;
    Console.WriteLine($"PASS {description}");
}
StudyHistoryRS Bar(long time, decimal side = 0, decimal otherSide = 0) => new()
{
    StudyColId = 1, RawTime = time, Close = 100,
    rawStudies = new() { ["test_SIDE"] = side, ["other_SIDE"] = otherSide }
};
var database = new MemorySignals();
void Commit(StudySignalBatch batch) => database.Commit(batch);
StudySignalBatch Calculate(IEnumerable<StudyHistoryRS> bars, long completedBefore = long.MaxValue)
{
    var batch = new StudySignalBatch(["test"], database.ProcessedThrough, database.ReadLatest);
    batch.Process(bars, completedBefore);
    return batch;
}
string Rows() => JsonSerializer.Serialize(database.Signals);

// A checkpoint at bar 30 is older than the committed history/signal frontier at 45.
var history = Enumerable.Range(1, 45).Select(i => Bar(i, i is >= 32 and < 38 ? 1 : i >= 42 ? -1 : 0)).ToList();
var initial = Calculate(history);
Check(initial.Changes.Count == 2 && initial.ProcessedThrough == 45, "Initial batch creates two distinct signals and records every completed bar");
Commit(initial);
var originalRows = Rows();
var replay = Calculate(history.Skip(27));
Check(replay.Changes.Count == 0 && replay.ProcessedThrough == 45, "Restart replay from checkpoint 30 produces zero new or updated signals through bar 45");
Check(Rows() == originalRows, "Restart replay preserves original signal IDs, tags, prices and status");
var corrected = Calculate(history.Skip(27).Select(b => Bar(b.RawTime, b.RawTime % 2 == 0 ? 1 : -1)));
Check(corrected.Changes.Count == 0, "Changed study results on already processed bars cannot create or reverse trades");
Check(Calculate([Bar(45, 1)]).Changes.Count == 0, "The exact last processed bar is excluded, not only earlier bars");

var newBar = Calculate([Bar(44, 1), Bar(45, 1), Bar(46, 1)]);
Check(newBar.Changes.Count == 2 && newBar.Changes.Count(s => s.Id == -1) == 1,
    "Only the new bar can close the active signal and create one replacement");
Commit(newBar);
var afterNew = Rows();
Check(Calculate([Bar(46, -1)]).Changes.Count == 0 && Rows() == afterNew,
    "A retry after a successful commit with a lost acknowledgement cannot create another trade");

var failed = Calculate([Bar(47, 0)]); // discarded, as a rolled-back transaction would be
Check(failed.Changes.Count == 1 && failed.ProcessedThrough == 47 && database.ProcessedThrough == 46,
    "Uncommitted processing never advances durable signal progress");
Check(Rows() == afterNew, "A failed batch cannot mutate previously committed signal objects");
var retry = Calculate([Bar(47, 0)]); Commit(retry);
Check(database.Signals.Count == 3 && database.Signals.Last().Status == SignalStatus.Stop,
    "Retry after rollback closes the existing signal without creating a duplicate");
Check(Calculate([Bar(47, 1)]).Changes.Count == 0, "A replay cannot reopen a signal closed on the same bar");

var flat = Calculate([Bar(48), Bar(49)]); Commit(flat);
Check(flat.Changes.Count == 0 && database.ProcessedThrough == 49, "Flat bars advance signal progress even when no signal is written");
Check(Calculate([Bar(48, 1), Bar(49, -1)]).Changes.Count == 0, "A correction to a previously flat bar cannot create a historical trade");

var forming = Calculate([Bar(50, 1)], completedBefore: 50); Commit(forming);
Check(forming.Changes.Count == 0 && database.ProcessedThrough == 49, "Saving a forming bar does not mark its signals as processed");
var closed = Calculate([Bar(50, 1)], completedBefore: 51); Commit(closed);
Check(closed.Changes.Count == 1 && database.ProcessedThrough == 50, "A bar that closes during a restart emits its first signal once");
Check(Calculate([Bar(50, -1)], 51).Changes.Count == 0, "Repeated closed-bar processing cannot generate a second signal");

var deletion = Calculate(Enumerable.Range(1, 50).Select(i => Bar(i, i % 2 == 0 ? -1 : 1)));
Check(deletion.Changes.Count == 0 && deletion.ProcessedThrough == 50,
    "Rebuilding deleted history cannot rewind independently persisted signal progress");

// Signal rows provide a second guard if progress was imported from older state.
var existingClosed = new SignalRS { Id = 90, Strategy = "test", Tag = "existing", Status = SignalStatus.Stop,
    Side = 1, StartTime = 35, StopTime = 44, StartPrice = 100, StopPrice = 101, LastUpdated = 44 };
var legacy = new StudySignalBatch(["test"], 30, _ => existingClosed);
legacy.Process(Enumerable.Range(31, 14).Select(i => Bar(i, i % 2 == 0 ? 1 : -1)), long.MaxValue);
Check(legacy.Changes.Count == 0 && legacy.ProcessedThrough == 44,
    "Latest closed signal prevents duplicate starts and stops up to its last event");
Check(existingClosed.Status == SignalStatus.Stop && existingClosed.Tag == "existing", "Replay does not resave or reopen closed signal records");
var legacyActive = new StudySignalBatch(["test"], 30, _ => new SignalRS(existingClosed)
    { Status = SignalStatus.Start, StartTime = 44, StopTime = null });
legacyActive.Process([Bar(44, -1)], long.MaxValue);
Check(legacyActive.Changes.Count == 0, "An existing start timestamp prevents another start on the same bar");

var scoped = new StudySignalBatch(["test", "test"], -1, _ => null);
scoped.Process([Bar(1, 1, 1), Bar(1, -1, -1)], long.MaxValue);
Check(scoped.Changes.Count == 1 && scoped.Changes[0].Strategy == "test", "Duplicate bars/config names and unassigned strategies cannot create extra signals");
var independent = new StudySignalBatch(["test", "other"], -1, _ => null);
independent.Process([Bar(1, 1, -1)], long.MaxValue);
Check(independent.Changes.Count == 2 && independent.Changes.Select(s => s.Strategy).Distinct().Count() == 2,
    "Two assigned strategies can each emit one legitimate signal for the same bar");
var noOutput = new StudySignalBatch(["test"], 50, _ => throw new Exception("No signal lookup expected"));
noOutput.Process([new StudyHistoryRS { RawTime = 51, rawStudies = new() }], long.MaxValue);
Check(noOutput.ProcessedThrough == 51 && noOutput.Changes.Count == 0, "Warmup bars without signal keys still record completed processing");

// Repeat restart/replay boundaries over a longer alternating sequence, including multiple strategies.
var longHistory = Enumerable.Range(1, 250).Select(i => Bar(i, i % 11 < 4 ? 0 : i % 11 < 8 ? 1 : -1,
    i % 13 < 6 ? -1 : 0)).ToList();
var referenceDb = new MemorySignals();
foreach (var row in longHistory)
{
    var batch = new StudySignalBatch(["test", "other"], referenceDb.ProcessedThrough, referenceDb.ReadLatest);
    batch.Process([row], long.MaxValue); referenceDb.Commit(batch);
}
var restartDb = new MemorySignals();
int publications = 0;
for (int end = 7; end <= 257; end += 7)
{
    int last = Math.Min(end, 250);
    int checkpoint = Math.Max(0, (int)restartDb.ProcessedThrough / 30 * 30 - 3);
    var batch = new StudySignalBatch(["test", "other"], restartDb.ProcessedThrough, restartDb.ReadLatest);
    batch.Process(longHistory.Skip(checkpoint).Take(last - checkpoint), long.MaxValue);
    publications += batch.Changes.Count(s => s.Id == -1);
    restartDb.Commit(batch);
    if (last == 250) break;
}
string Events(MemorySignals db) => JsonSerializer.Serialize(db.Signals.Select(s => new { s.Strategy, s.Side, s.StartTime, s.StopTime, s.Status }));
Check(Events(restartDb) == Events(referenceDb), "Repeated restarts from 30-bar checkpoints preserve the same complete signal lifecycle");
Check(publications == referenceDb.Signals.Count, "Repeated replays create no additional trade-driving signal identities");
Check(restartDb.Signals.GroupBy(s => (s.Strategy, s.StartTime)).All(g => g.Count() == 1),
    "Each strategy/bar start occurs at most once across all replay boundaries");

var adaptiveDb = new MemorySignals();
int adaptivePublications = 0;
for (int end = 7; end <= 257; end += 7)
{
    int last = Math.Min(end, 250);
    // Exercise both a 100-bar recovery gap and switching back and forth to 30.
    int interval = last < 150 || last >= 220 ? 100 : 30;
    int checkpoint = Math.Max(0, (int)adaptiveDb.ProcessedThrough / interval * interval - 3);
    var batch = new StudySignalBatch(["test", "other"], adaptiveDb.ProcessedThrough, adaptiveDb.ReadLatest);
    batch.Process(longHistory.Skip(checkpoint).Take(last - checkpoint), long.MaxValue);
    adaptivePublications += batch.Changes.Count(s => s.Id == -1);
    adaptiveDb.Commit(batch);
    if (last == 250) break;
}
Check(Events(adaptiveDb) == Events(referenceDb), "Switching recovery gaps between 100 and 30 preserves the complete signal lifecycle");
Check(adaptivePublications == referenceDb.Signals.Count
    && adaptiveDb.Signals.GroupBy(s => (s.Strategy, s.StartTime)).All(g => g.Count() == 1),
    "100/30-bar restart replay generates no duplicate trade-driving signals");

Console.WriteLine($"{checks} signal replay checks passed.");
if (args.Contains("--sql"))
{
    try { await SqlSignalReplayChecks.Run(Check); }
    catch (Exception ex)
    {
        // Return a test failure normally; an unhandled Windows exception may wait for a JIT debugger.
        Console.Error.WriteLine($"SQL integration failed: {ex}");
        Environment.ExitCode = 1;
    }
}

sealed class MemorySignals
{
    public long ProcessedThrough { get; private set; } = -1;
    public List<SignalRS> Signals { get; } = [];
    public SignalRS? ReadLatest(string strategy) => Signals.Where(s => s.Strategy == strategy)
        .OrderByDescending(s => s.StartTime).ThenByDescending(s => s.Id).Select(s => new SignalRS(s)).FirstOrDefault();
    public void Commit(StudySignalBatch batch)
    {
        foreach (var change in batch.Changes)
        {
            var stored = new SignalRS(change) { LastUpdated = batch.ProcessedThrough };
            if (stored.Id == -1) { stored.Id = Signals.Count + 1; Signals.Add(stored); }
            else Signals[Signals.FindIndex(s => s.Id == stored.Id)] = stored;
        }
        ProcessedThrough = batch.ProcessedThrough;
    }
}
