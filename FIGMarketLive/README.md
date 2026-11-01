# FIGMarketLive — first implementation

Windows tray host for managed FIGSignalSvc study collections, with native StudyHistory checkpoint capture and restoration. It catches up monitored datasets whenever Price Sync starts, then captures intraday prices during each SQL exchange schedule, resumes compatible study collections, and publishes binary StudyHistory values. It is not yet the complete broker-to-chart application.

## Build and launch

```powershell
dotnet build FIGMarketLive.slnx -c Release -m:1
dotnet run --project tests/FIGMarketLive.Regression -c Release --no-build
```

Launch `FIGMarketLive/bin/Release/net10.0-windows/FIGMarketLive.exe` with an optional absolute configuration-file argument. This is also the initial process-launch contract for FIGCharts; the chart launcher itself has not been added. The default configuration is `marketlive.json` beside the executable. One instance per Windows session is allowed. A second launch exits. The window closes to the tray; Exit cancels and awaits the worker.

Click **Configuration** for database, adapter, ticker/dataset, study and collection-type setup. IBKRProvider is embedded through the provider-neutral IMarketDataAdapter interface. Opening Configuration or Tools keeps monitoring running; confirmed dataset changes or manual imports stop the workers before writing. Start loads all enabled StudyCol rows from SQL and automatically selects an exact dataset or compatible aggregate source. JSON contains local connection, adapter, plugin-location and polling settings only; Monitors and DataSelections are legacy migration inputs. Relative plugin paths resolve against the configuration directory. Polling reloads SQL definitions; restart to load different plugin packages or local settings. See [configuration workflow](CONFIGURATION.md).

Point PluginPath at the FIGSignalSvc plugin root containing collection directories and `plugin.json` manifests. Keep each package's dependencies/rules beside it as supplied. FIGMarketLive.Core compiles the exact existing `FIGSignalSvc/Plugins/*.cs` loader via linked sources, without referencing the service executable or changing its behavior. Compatible FIG.Studies.Core, FIG.Studies.Common and FIGCommon assemblies are shared with the host. Native optimizer DLLs are not the managed service plugins.

## Database contract

Use a dedicated database with Ticker, Interval, DataSet, PriceData and StudyCol from the supplied schema. Database testing/startup creates binary StudyHistory storage. StudyHistory.Studies holds FIGS binary outputs; CheckpointState separately holds native JSON restart state. BarStudy is obsolete. Legacy consolidation preserves output bytes/checkpoints, converts supported JSON values with the codec, verifies the copy, then drops BarStudy in the same transaction. Unsupported values/dependencies abort migration. The explicit --reset-study-storage command instead truncates StudyHistory, clears regeneration progress and removes BarStudy; use it only when discarding all study results is intended. PriceData and definitions are retained. This is FIGMarketLive's binary database contract; JSON-based consumers must use a binary decoder.

StudyCol, ColType, Ticker, DataSet and Interval drive runtime configuration. Disabled studies are excluded. An exact source interval is preferred; otherwise the largest compatible base interval is chosen, with dataset ID breaking ties. Incompatible studies are reported independently while valid studies continue. Plugin manifests select the runtime DLL; ColType is automatically synchronized SQL metadata. Existing SQL PriceData and PriceDataRS still use int Volume; overflow during summation fails explicitly.

OHLCV aggregation uses UTC opening timestamps: first open, maximum high, minimum low, last close and checked summed volume. Fixed periods use Unix-epoch boundaries, days use UTC midnight, weeks start Monday, and months/years use calendar boundaries. Multi-period groups share epoch-based anchors. Partial first/current bars are retained; source gaps do not produce synthetic bars. Misaligned source bars are rejected rather than split. Exchange-session/trading-day timezone aggregation is not implemented. Aggregates feed study calculation and StudyHistory publication without inserting extra aggregate DataSet/PriceData rows.

Publication uses SqlBulkCopy staging and serializable transactions. Small updates publish atomically; large histories and Tools → Regenerate Studies commit configured batches (default 1,000 bars), including binary outputs, checkpoint history and durable regeneration progress. Each batch validates SQL configuration and its raw source window; completion rechecks the full source fingerprint and output coverage. Source changes reject the affected batch without discarding earlier commits. Interrupted runs resume compatible checkpoints. No historical outputs are silently deleted. The SQL timeout defaults to 360 seconds and is configurable alongside batch size in Database settings. Production publication against the application database has not been run during verification.

PriceData changes write per-dataset Event rows through a set-based trigger in the same transaction. FIGCommon EventMonitorService wakes affected studies via SqlDependency; repeated events coalesce. Startup and backup reconciliation still check all enabled studies, covering missed notifications. PollSeconds is the backup interval (default 60 seconds for new settings), configurable under Database. Database testing verifies broker status, permissions and actual notification delivery. Price-request pacing is independent. Checkpoint-capable collections restore the newest compatible completed-bar snapshot and replay only the suffix, including the forming bar. Older price corrections select an earlier snapshot; incompatible state or unsupported DLLs use full replay. Checkpoints and binary outputs publish atomically. Defaults are CheckpointsEnabled=true and CheckpointEveryBars=5000, configurable in Database. Regeneration commits at the configured batch size independently of checkpoint cadence, retaining every captured state; the serialized-checkpoint memory threshold can trigger an early commit. Source input history is still scanned for integrity when a dataset is processed. Only one calculation runs at a time; one monitor's failure is logged and other monitors continue. Configure one host as the owner of each StudyCol.

Logs are in `%LOCALAPPDATA%/FIGMarketLive/marketlive.log`; the current implementation does not rotate them. The tray dashboard shows calculation status/errors. Plugins are trusted application code, run in process; reload DLL builds by restarting the host.

## Binary format: FIGS version 1

StudyHistory.Studies is `varbinary(max)`. Bytes represent published values, never collection objects, CLR memory or checkpoints. The initial self-describing format repeats names per row to handle sparse/dynamic outputs correctly; schema-ID/name-deduplication is deferred until profiling justifies it.

All numbers are little endian. The header is ASCII `FIGS`, uint16 version 1, int32 entry count. Entries are sorted by ordinal key. Each entry is an int32 UTF-8 byte length, key bytes, a byte type tag, and its value:

| Tag | Value |
| --- | --- |
| 0 | null, no bytes |
| 1 | IEEE754 float64 |
| 2 | decimal: four int32 words from .NET decimal.GetBits (lo, mid, hi, flags) |
| 3 | signed int64 |
| 4 | boolean, byte 0 or 1 |
| 5 | int32 UTF-8 byte length and string bytes |
| 6 | signed int32 |
| 7 | IEEE754 float32 |

NaN/infinity are preserved; consumers should display them as unavailable rather than plot them. Missing keys and present null keys remain distinct. Unknown CLR types are rejected rather than silently converted. Decoder rejects malformed lengths, invalid UTF-8, duplicate keys, unknown versions/tags, truncation and trailing bytes. Limits: 100,000 entries, 16 MiB payload. No compression is applied. This is not the columnar FIGDATA file format; FIGCharts requires a decoder/live-source adapter before consuming it.

## Remaining integration

1. Further concurrent-writer stress testing; isolated transactional publication and stale-source rejection tests pass.
2. Real collection/configuration parity and replay performance measurements.
3. Live verification of the intraday price-sync worker; stronger session-aware catch-up coverage and shared provider pacing. See CONFIGURATION.md for capture limits.
4. Full database bootstrap/migrations beyond the additive provider/settings migrations.
5. Live SQL verification of the e-Signal Tools importer; exchange-session aggregation rules (UTC aggregation is implemented).
6. Production measurements of checkpoint recovery with deployed collection packages.
7. Revisioned chart IPC, native decoder, chart launch integration and live workspaces.

No trading/signal execution is invoked. FIGSignalSvc source behavior is unchanged; MarketLive reuses its native checkpoint persistence implementation.

## Validation performed

Release build of FIGMarketLive.slnx succeeded with zero warnings/errors. The regression runner passed 376 database-free checks. Fifty-one isolated SQL checks cover checkpoint recovery/parity, atomic publication, stale-source rejection, shared/last-ticker deletion, catch-up on each Start and schedule enforcement afterward, including closure during retrieval. SQL checks also verify safe Broker enablement, explicit administrator SINGLE_USER setup with rollback and MULTI_USER restoration, actual EventMonitorService delivery, transaction rollback and multi-dataset notifications. Regeneration checks cover batch cancellation/resume, binary parity, source-change rejection/recovery and disabled StudyCols. Configuration UI and shared event-monitor regression checks also passed. These synthetic tests do not establish production feed or collection performance.

An optional real-plugin test accepts an output folder, a plugin directory containing exactly one manifest, and a parameter JSON file:

```powershell
dotnet run --project tests/FIGMarketLive.Regression -c Release --no-build -- .test-temp/marketlive PATH_TO_PLUGIN_DIRECTORY tests/FIGMarketLive.Regression/default-parameters.json
```

The existing `FIGSignalCollection/FIG.StudyCollections.LADX/publish` DLL passed this test using `{}` defaults: 204 total checks for the initial implementation, including sequential-vs-batch parity and binary round trips for its actual outputs. Its adjacent source-tree `ladx-parameters.json` was rejected by that published DLL for unknown setting `adxStrongThreshold`; matching plugin builds and parameter versions are required. No plugin DLL or parameter file was changed. These tests use synthetic bars and do not establish production performance or parity with an external feed. SQL settings migrations and monitor-loading dry runs have been verified; publication writes and live broker data retrieval remain unverified. Additional adapter, aggregation and configuration checks are described in CONFIGURATION.md.
