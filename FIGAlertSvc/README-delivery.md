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
