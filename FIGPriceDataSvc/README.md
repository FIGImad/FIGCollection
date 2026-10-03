# FIGPriceDataSvc

FIGPriceDataSvc retrieves historical prices through FIGControllerSvc from FIGPriceSyncSvc and writes them into its own PriceData table. It does not load study plugins or connect directly to IBKR.

## Startup fix

The copied Program previously registered only RemotePriceSyncOptions. It did not register RemotePriceSyncService, its store/API dependencies, or the controller connection. Both settings files also lacked RemotePriceSync configuration.

Program now calls AddPriceDataServices, which registers the controller client and background worker. The standalone service is enabled by default. Explicit RemotePriceSync:Enabled=false disables price retrieval and produces a warning.

Startup logs include "Remote price worker started". On .NET 10, ExecuteAsync runs on a background thread; set a breakpoint there before starting the process.

## Configuration

Both pricedata-settings.json and pricedata-settings.development.json now contain RemotePriceSync:

- Enabled: true.
- DestinationServiceId: empty means route by the PriceSync role. Set the exact remote FIGPriceSyncSvc registration ID when more than one service advertises that role.
- DataSetIds: empty means use all one-minute datasets in this service's local database. Set explicit local IDs to restrict synchronization.
- TimeOffsetSec: 5, matching the checked-in FIGPriceSyncSvc provider configuration.
- InitialHistoryDays: 14.
- OverlapBars: 3.
- WindowBars: 1000.
- MarketSchedule: Central Standard Time, daily break 16:00–17:00 and weekly break Friday 16:00–Sunday 17:00, matching the checked-in provider configuration.

Use a distinct ControllerConfig registration identity for FIGPriceDataSvc; do not reuse the remote price provider's service ID. Existing controller credentials and local database connection settings still apply. Changes require a service restart. If provider timing or market hours are overridden elsewhere, apply the same values here.

## Timing

When the market is open, initial catch-up processes every historical window up to the pass's target time, committing them oldest first. Corrections cannot run until catch-up succeeds. Subsequent requests use UTC clock boundaries rather than a delay measured from the previous response:

- Fetch at each minute boundary minus TimeOffsetSec seconds minus 500 milliseconds.
- Correct existing bars at the following minute boundary.

For TimeOffsetSec=5 this is 12:00:54.500, then 12:01:00.000, then 12:01:54.500, and so on. The first request includes the current forming bar. The correction refreshes bars already present before the request and does not append a new minute if the response is late.

This replaces the copied 15-second, completed-bars-only loop. PollSeconds is no longer used by FIGPriceDataSvc. Market-break calculations are shared with FIGPriceSyncSvc. Network/provider latency can delay completion; the schedules do not imply simultaneous database commits or identical provider snapshots.

Each dataset is fetched sequentially to avoid overlapping local requests to the provider API. Every normal fetch drains all required historical windows before allowing corrections. Concurrent calls for the same dataset are serialized. Failed windows are retried after 75 seconds; corrections remain blocked until catch-up succeeds. A correction is skipped if catch-up is at least one minute stale. No price database migration is needed.

Catch-up resumes from the latest persisted bar with overlap. It prevents new jumps to recent data but does not repair pre-existing gaps older than that overlap; those require deliberate backfill.

## Validation and running

Timeout constants are members of `RemotePriceSyncService`: `ProviderTimeoutMs` (45 seconds), `ControllerRouteTimeoutMs` (60 seconds), `ResponseTimeoutMs` (65 seconds), and `CatchUpRetryDelayMs` (75 seconds). The retry delay starts after a failure and is derived from the response timeout plus 10 seconds. Both initial and later catch-up failures use it. Provider and routing budgets are carried in the request; deploy the updated FIGPriceSyncSvc and FIGControllerSvc to honor changes to those budgets. Existing clients retain their server defaults. Remote timeout budgets are bounded to 1–300 seconds.

The regression suite now targets the extracted FIGPriceDataSvc project and tests real host startup with fake transport, bar timing, forming-bar retrieval, update-only correction, and SQL writes against disposable databases built from FIGAutoTrader_Schema.sql.

    dotnet run --project tests/FIGRemotePrices.Regression -- --sql

If your debugger is running FIGPriceDataSvc, stop that session before rebuilding its normal output directory. A running process locks FIGCommon.dll and other assemblies. Restart the debug session to load the changed registration and scheduler.

See [API and local price synchronization details](PriceSync/README.md).
