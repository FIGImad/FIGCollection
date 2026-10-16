# Two alert service instances

One FIGAlertSvc executable supports `AlertProcessing:Mode` values `Combined`
(the default), `SystemOnly`, and `OtherOnly`. Mode changes require a restart.
Use `--settings-file <path>` to select a configuration file and
`--windows-service-name <name>` to match its Windows service registration.

The supplied configurations use different controller IDs, service names, ports,
and log filenames. `alerts-settings.system.json` registers the `SystemAlert`
role (512) on port 5013; `alerts-settings.other.json` registers `Alert` (256)
on port 5003. Change these IDs if deploying additional pairs on the same controller.
Both use the same FIGAlert database and the existing shared credentials/settings.

## Deployment

1. Stop the existing combined FIGAlertSvc and the SystemAlert-producing
   FIGAutoTraderSvc during the cutover.
2. Apply `FIGInstaller/Resources/fig_alert_service_split_migration.sql` in the
   FIGAlert database. It requires the current immediate delivery/timezone and
   recipient-enabled schema. The migration preserves data and can be rerun.
   On its first run it backfills legacy ALT/ALERT messages with the known
   SystemAlert header; review this categorization if other producers used that
   same header. Newly received records use the original logger identity, so
   ordinary log alerts and errors from the monitor are classified separately.
3. Build/publish FIGAlertSvc and deploy the updated FIGCommon with
   FIGAutoTraderSvc. The controller's role selection needs no code change, but
   deploy updated FIGCommon to the controller too so it recognizes the new role.
4. In FIGAutoTraderSvc's settings, add:

   ```json
   "LogAlerts": { "SeparateSystemAlerts": true }
   ```

   Add the same setting to any other process that forwards SystemAlert-monitor
   records. Without it, routing retains its original combined behavior.
5. Remove the stopped old FIGAlertSvc Windows service registration. From an
   elevated PowerShell, run `Install-SplitServices.ps1 -PublishDirectory <folder>`
   with the published executable and both settings files. The script creates
   stopped service instances. It does not delete existing service registrations.
6. Start FIGSystemAlertSvc and FIGAlertSvc, then restart FIGAutoTraderSvc.
   Verify both controller registrations, categories, and delivery logs.

Do not run a Combined instance alongside the two split instances against the
same database, or run multiple instances of the same mode: their pending work
would overlap. Each split instance filters pending rows and age-based ignoring
in SQL before selecting an immediate batch. A request reaching the wrong
instance fails rather than being acknowledged and discarded.

The SystemAlert table monitor remains in FIGAutoTraderSvc. This split separates
alert ingestion/classification/delivery, retaining the existing producer and rules.
Existing notification retry semantics are unchanged: a partial delivery may resend
to recipients that already succeeded. No database migration, service installation,
or real notifications are performed by the regression harness.

## Verification

```powershell
dotnet build FIGAlertSvc/FIGAlertSvc.csproj --no-restore
dotnet run --project tests/FIGAlertSvc.Regression/FIGAlertSvc.Regression.csproj --no-restore
dotnet run --project tests/LogAlertRouter.Regression/LogAlertRouter.Regression.csproj --no-restore
```
