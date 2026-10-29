# FIGAlertSvc delivery update

Apply `Scripts/AlertMigration_DeliveryTimeZoneAndAge.sql` to the existing FIGAlert database before deploying the updated FIGAlertSvc and FIGCommon binaries. Reapply this repeatable migration for the batching update: both pending procedures now return IsImmediate so scheduled retries preserve immediate behavior. The full schema snapshot has also been updated; do not run that full create/seed script against an existing database.

## Recipient time zones

`dbo.Recipient.TimeZoneId` defaults to `UTC`. Set each recipient explicitly, for example:

```sql
UPDATE dbo.Recipient SET TimeZoneId = N'Eastern Standard Time' WHERE Id = 1;
-- Dubai: Arabian Standard Time
```

Eastern Standard Time is a Windows time-zone ID that automatically applies daylight saving. The message header includes the local date/time, UTC offset, and zone ID. Invalid zone IDs are logged as failures for the affected recipient. The recipient model and select/upsert procedures carry the preference; this change does not add an administration UI field.

Formatting happens at delivery, so the shared FriendlyMessage remains reusable. `{{alert_time}}` is also available in friendly-message templates. Existing `Source: ..., Time: yyyy-MM-dd HH:mm:ss.fff` headers are localized. Embedded signal START/STOP dates are preserved.

The known SystemAlertMonitorService forwards its original UTC event timestamp in the header. For newly ingested forwarded alerts, that timestamp is stored in Alert.RawTime/MSec instead of the later log/replay time. Other producers retain the log record timestamp. Already stored alert rows are not rewritten by the migration.

## Age limit

`AlertDispatch:MaxAlertAgeMinutes` in `alerts-settings.json` defaults to 30 and must be positive. Restart the service after changing it. Both scheduled and immediate SQL procedures receive this value and mark older pending matched alerts with IgnoredAt and IgnoreReason, leaving SentAt unset. The cutoff uses UTC epoch seconds, independent of recipient time zones. An alert exactly on the cutoff second remains eligible. Ignored alerts are not automatically revived if the limit is increased.

## Delivery and logs

Immediate alerts are sent individually, including retries through the scheduled dispatcher. Non-immediate alerts are batched by recipient and subscription method mask. Identical formatted messages appear once with `(Occurrences: N)`; different service names or formatted times remain distinct. Counts cover the current pending batch, not a historical window. Entries are packed up to 1,024 characters, including separators and occurrence labels, and split between entries. A single oversized entry is converted to plain text and split into numbered parts without cutting surrogate pairs. Short messages retain HTML formatting. Every part must receive an HTTP success and API status 1. Logs identify all alert IDs represented in each batch, recipient, part, HTTP/API status, and Pushover request ID; rejected responses include error details. API acceptance means queued by Pushover, not confirmed on the phone.

An alert is marked sent only after all returned recipient/channel sends succeed. Existing retry behavior remains: a partially successful alert may be resent to recipients or parts that already succeeded. This update does not add a persistent per-recipient delivery ledger. Alerts already marked sent by the previous version are not automatically replayed.

## Verification

`dotnet build FIGAlertSvc/FIGAlertSvc.csproj --no-restore`

`dotnet run --project tests/FIGAlertSvc.Regression/FIGAlertSvc.Regression.csproj`

The regression harness uses real dispatch, formatting, and Pushover code with in-memory database/SMTP boundaries and a simulated HTTP handler. It covers long messages, Unicode, API failures, partial recipient/part failures, recipient time zones/DST, disabled recipients, and configured age forwarding. SQL migration execution and live delivery require validation in the deployment environment.

## Split instances

See [README-split-services.md](README-split-services.md) for Combined, SystemOnly, and OtherOnly modes, migration, routing, and installation. Apply the split migration after older delivery/recipient migrations so pending queries retain category filtering.

## Acknowledged system-alert ingestion

SystemAlertMonitorService now reads pending source rows without changing DeliveryTime. It sends them directly through the controller to the SystemAlert role at POST /api/alert/systemrec, bypassing the LogAnalyzerSink queue. The endpoint returns the source SystemAlertId and stored AlertId only after persistence and rule processing succeed. Only a matching positive receipt allows the source DeliveryTime to be updated.

The monitor drains batches of 100 on startup and on SYSTEMALERT events, and retries pending rows every 30 seconds without requiring another event. Each forwarding attempt has a 30-second cancellation deadline. Controller, authentication, receiver, and database failures leave the row pending. A failure pauses the current batch until another wakeup. There is no pending-age cutoff at ingestion.

Retries are idempotent in the receiver database using a unique (ServiceId, SystemAlertId) key and a transactional lookup/insert. A lost response or a failed source acknowledgement reuses the existing Alert row without resetting SentAt. Keep the producer ControllerConfig.Id stable and do not reuse source record IDs. Deduplication lasts while the receiver Alert row is retained.

Deployment order:

1. Stop all old AutoTrader monitor instances: the old select procedure marks rows delivered before reception and skips unselected rows.
2. Apply ../FIGCommon/DataSchema/system_alert_delivery_receiver.sql to the Alert database, after the existing alert migrations.
3. Apply ../FIGCommon/DataSchema/system_alert_delivery_source.sql to the AutoTrader database.
4. Deploy FIGAlertSvc with FIGCommon, and ensure an instance accepting system alerts is registered with the controller under the SystemAlert role.
5. Deploy and start FIGAutoTraderSvc with FIGCommon. Pending DeliveryTime = 0 rows recover automatically.

The new reader uses a new procedure name so an unmigrated source database fails safely instead of invoking the old destructive selector. Existing nonzero DeliveryTime rows are not automatically replayed because their prior reception cannot be inferred. Do not run the full schema snapshots against existing databases.

This acknowledgement confirms durable ingestion by FIGAlertSvc. Recipient delivery remains subject to classification, subscriptions, and the existing age limit: a long outage can result in an alert being recorded but too old for email/Pushover dispatch.

Verification: the LogAlertRouter.Regression harness covers missing/malformed/mismatched receipts, receiver outages, authentication renewal, startup recovery, failed source acknowledgements, timer-based recovery without new events, and shutdown. Live SQL migrations and concurrent SQL deduplication must also be validated in the deployment environment.
