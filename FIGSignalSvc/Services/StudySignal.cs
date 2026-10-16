using FIGCommon.DataAccess;
using FIGCommon.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics;

namespace FIGSignalExSvc.Services
{
    public class StudySignal
    {
        private readonly ILogger<StudySignal> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly StudyColRS studyColRec;
        private readonly IntervalRS studyInterval;
        private readonly string[] strategies;
        private readonly int timeOffset;
        private readonly TimeProvider clock;
        private readonly StudySignalStore signalStore = new();

        public StudySignal(StudyColRS studyCol, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<StudySignal>>();
            studyColRec = new StudyColRS(studyCol);
            studyInterval = MainRepo.GetInterval(studyCol.IntervalId)
                ?? throw new InvalidDataException($"Study interval {studyCol.IntervalId} was not found.");
            if (studyInterval.IntervalLen <= 0) throw new InvalidDataException("Signal interval must be positive.");
            strategies = MainRepo.QueryStrategyConfigs(studyCol.Id).Select(c => c.Strategy).ToArray();
            timeOffset = serviceProvider.GetRequiredService<IConfiguration>().GetValue("SingalProcessing:TimeOffset", 5);
            clock = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
        }

        public void ProcessSignals(List<StudyHistoryRS> studyHistory, bool updateOnly,
            List<StudyHistoryRS>? precedingUpdates = null, Action<SqlTransaction>? saveCheckpoint = null,
            Action<SqlTransaction>? saveHistory = null)
        {
            // Freeze the bar boundary across retries; only durable state is reloaded.
            long effectiveNow = clock.GetUtcNow().ToUnixTimeSeconds() + timeOffset;
            long completedBefore = effectiveNow / studyInterval.IntervalLen * studyInterval.IntervalLen;
            bool changed = false;
            try
            {
                SignalDeadlockRetry.Execute(
                () => changed = ProcessSignalsAttempt(studyHistory, updateOnly, precedingUpdates,
                    saveCheckpoint, saveHistory, completedBefore),
                (retry, milliseconds, error) => _logger.LogWarning(error,
                    "StudyCol {Id}: SQL deadlock victim; retry {Retry}/3 of the entire transaction in {Milliseconds} ms",
                    studyColRec.Id, retry, milliseconds));
            }
            catch (SqlException ex) when (SignalDeadlockRetry.IsDeadlock(ex))
            {
                _logger.LogError(ex, "StudyCol {Id}: deadlock retries exhausted after 4 transaction attempts", studyColRec.Id);
                throw;
            }
            // Publication is outside the retry boundary: a successful commit is never retried
            // because of a subsequent notification failure.
            if (changed)
                _serviceProvider.GetService<FIGSignalExSvc.Publication.SignalPublicationWakeup>()?.Notify();
        }

        private bool ProcessSignalsAttempt(List<StudyHistoryRS> studyHistory, bool updateOnly,
            List<StudyHistoryRS>? precedingUpdates, Action<SqlTransaction>? saveCheckpoint,
            Action<SqlTransaction>? saveHistory, long completedBefore)
        {
            // Process all rows supplied by checkpoint recovery. The durable progress marker,
            // not the newest history row or checkpoint, decides which rows may emit signals.
            var candidates = precedingUpdates != null
                ? precedingUpdates.Concat(studyHistory).ToList()
                : updateOnly ? studyHistory.TakeLast(1).ToList() : studyHistory;
            SqlTransaction? tx = null;
            SqlConnection? connection = null;
            try
            {
                tx = MainRepo.OpenTransaction()
                    ?? throw new InvalidOperationException("Cannot persist studies and signals without a database transaction.");
                connection = tx.Connection;
                var progress = signalStore.ReadProgressForUpdate(studyColRec.Id, tx);
                var batch = new StudySignalBatch(strategies, progress, name => signalStore.ReadLatestSignal(name, tx));
                batch.Process(candidates, completedBefore);

                if (saveHistory != null) saveHistory(tx);
                else if (precedingUpdates != null)
                {
                    if (precedingUpdates.Count > 0) MainRepo.UpdateStudyHistory(precedingUpdates, tx);
                    if (studyHistory.Count > 0) MainRepo.BulkInsertStudyHistory(studyHistory, tx);
                }
                else if (updateOnly) MainRepo.UpdateStudyHistory(candidates, tx);
                else MainRepo.BulkInsertStudyHistory(studyHistory, tx);

                foreach (var signal in batch.Changes)
                {
                    _logger.LogInformation("Upserting Signal for Strategy {Strategy} with Side {Side} and Status {Status}",
                        signal.Strategy, signal.Side, signal.Status);
                    var writeWatch = Stopwatch.StartNew();
                    try { MainRepo.UpsertSignal(signal, tx); }
                    finally
                    {
                        _logger.LogInformation("Signal upsert call ended for StudyCol {Id}, Strategy {Strategy}, Signal {SignalId}: {Milliseconds} ms (transaction not yet committed)",
                            studyColRec.Id, signal.Strategy, signal.Id, writeWatch.ElapsedMilliseconds);
                    }
                }
                signalStore.SaveProgress(studyColRec.Id, progress, batch.ProcessedThrough, tx);
                saveCheckpoint?.Invoke(tx);
                var commitWatch = Stopwatch.StartNew();
                try { tx.Commit(); }
                finally
                {
                    _logger.LogInformation("StudyCol {Id}: transaction commit call ended after {Milliseconds} ms",
                        studyColRec.Id, commitWatch.ElapsedMilliseconds);
                }
                return batch.Changes.Count > 0;
            }
            catch (Exception ex)
            {
                if (SignalDeadlockRetry.IsDeadlock(ex))
                    _logger.LogWarning(ex, "StudyCol {Id}: transaction was rolled back by SQL Server after a deadlock", studyColRec.Id);
                else
                {
                    _logger.LogError(ex, "Error committing study history, signals and signal progress; rolling back");
                    try { tx?.Rollback(); }
                    catch (Exception rollbackError) { _logger.LogError(rollbackError, "Rollback failed"); }
                }
                // No signal state survives a failed or uncertain commit. The next attempt
                // reloads durable progress and signal rows under the transaction lock.
                throw;
            }
            finally
            {
                tx?.Dispose();
                connection?.Dispose();
            }
        }

        public static T? Get<T>(object? value)
        {
            // If the value is already of type T
            if (value is T tValue)
            {
                return tValue;
            }

            // Handle null values
            if (value == null)
            {
                return default;
            }

            // Special handling for numeric conversions
            try
            {
                Type targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                // Handle decimal/double conversions explicitly
                if (targetType == typeof(decimal) && value is double d)
                {
                    return (T)(object)Convert.ToDecimal(d);
                }

                if (targetType == typeof(decimal) && value is float f)
                {
                    return (T)(object)Convert.ToDecimal(f);
                }

                // Enum from string or int
                if (targetType.IsEnum)
                {
                    if (value is string s)
                        return (T)Enum.Parse(targetType, s, ignoreCase: true);

                    if (IsNumericType(value.GetType()))
                        return (T)Enum.ToObject(targetType, value);
                }

                // Guid from string
                if (targetType == typeof(Guid) && value is string guidStr)
                    return (T)(object)Guid.Parse(guidStr);

                // DateTimeOffset from string
                if (targetType == typeof(DateTimeOffset) && value is string dtoStr)
                    return (T)(object)DateTimeOffset.Parse(dtoStr);

                // Handle other conversions
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return default;
            }
        }

        private static bool IsNumericType(Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or
                TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or
                TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or
                TypeCode.Double or TypeCode.Single => true,
                _ => false,
            };
        }

    }
}
