using FIGCommon.Models.Alert;

namespace FIGAlertSvc.Services;

internal sealed record AlertBatch(List<PendingAlertRS> Rows, string Message);

internal static class AlertBatchBuilder
{
    internal static List<AlertBatch> Build(List<PendingAlertRS> rows, bool immediate)
    {
        if (immediate)
            return rows.Select(row => new AlertBatch([row], AlertMessageFormatter.Format(row))).ToList();
        var batches = new List<AlertBatch>();
        foreach (var recipient in rows.GroupBy(r => new { r.RecipientId, r.AlertMethods }))
        {
            var scheduled = new List<PendingAlertRS>();
            foreach (var row in recipient)
            {
                if (immediate || row.IsImmediate)
                    batches.Add(new AlertBatch([row], AlertMessageFormatter.Format(row)));
                else
                    scheduled.Add(row);
            }

            var batchRows = new List<PendingAlertRS>();
            string body = "";
            // Group after formatting: different service names or displayed times remain distinct.
            foreach (var duplicates in scheduled.GroupBy(AlertMessageFormatter.Format, StringComparer.Ordinal))
            {
                var entryRows = duplicates.ToList();
                string entry = duplicates.Key + (entryRows.Count > 1 ? $"\n(Occurrences: {entryRows.Count})" : "");
                if (batchRows.Count > 0 && body.Length + 2 + entry.Length > 1024)
                {
                    batches.Add(new AlertBatch(batchRows, body));
                    batchRows = [];
                    body = "";
                }
                body += (batchRows.Count == 0 ? "" : "\n\n") + entry;
                batchRows.AddRange(entryRows);
                // A single oversized entry is split safely by the Pushover sender.
            }
            if (batchRows.Count > 0) batches.Add(new AlertBatch(batchRows, body));
        }
        return batches;
    }
}
