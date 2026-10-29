# FIGMarketLive — first implementation

Windows tray host for managed FIGSignalSvc study collections, without checkpoint capture or restoration. This first slice monitors **existing SQL PriceData**, reconstructs collections on price changes, and publishes binary BarStudy values. It is not yet the complete broker-to-chart application.

## Build and launch

```powershell
dotnet build FIGMarketLive.slnx -c Release -m:1
dotnet run --project tests/FIGMarketLive.Regression -c Release --no-build
```

Launch `FIGMarketLive/bin/Release/net10.0-windows/FIGMarketLive.exe` with an optional absolute configuration-file argument. This is also the initial process-launch contract for FIGCharts; the chart launcher itself has not been added. The default configuration is `marketlive.json` beside the executable. One instance per Windows session is allowed. A second launch exits. The window closes to the tray; Exit cancels and awaits the worker.

Click **Configuration** for database, adapter, ticker/dataset, study and collection-type setup. IBKRProvider is embedded through the provider-neutral IMarketDataAdapter interface. Opening configuration pauses monitoring. Start loads all enabled StudyCol rows from SQL and automatically selects an exact dataset or compatible aggregate source. JSON contains local connection, adapter, plugin-location and polling settings only; Monitors and DataSelections are legacy migration inputs. Relative plugin paths resolve against the configuration directory. Polling reloads SQL definitions; restart to load different plugin packages or local settings. See [configuration workflow](CONFIGURATION.md).

Point PluginPath at the FIGSignalSvc plugin root containing collection directories and `plugin.json` manifests. Keep each package's dependencies/rules beside it as supplied. FIGMarketLive.Core compiles the exact existing `FIGSignalSvc/Plugins/*.cs` loader via linked sources, without referencing the service executable or changing its behavior. Compatible FIG.Studies.Core, FIG.Studies.Common and FIGCommon assemblies are shared with the host. Native optimizer DLLs are not the managed service plugins.

## Database contract

Use a dedicated database with Ticker, Interval, DataSet, PriceData and StudyCol from the supplied schema. For a new deployment, omit the original BarStudy definition and create it using `Database/CreateBarStudy.sql`. This script is manual, creates only a missing BarStudy table, and refuses an existing text Studies column. It does not install SQL Server, create a database, migrate existing JSON, or run automatically. Do not use SQL CAST of JSON text to binary: those bytes would not be the FIGS format.

StudyCol, ColType, Ticker, DataSet and Interval drive runtime configuration. Disabled studies are excluded. An exact source interval is preferred; otherwise the largest compatible base interval is chosen, with dataset ID breaking ties. Incompatible studies are reported independently while valid studies continue. ColType.DLLName must match the installed managed plugin registration. Existing SQL PriceData and PriceDataRS still use int Volume; overflow during summation fails explicitly.

OHLCV aggregation uses UTC opening timestamps: first open, maximum high, minimum low, last close and checked summed volume. Fixed periods use Unix-epoch boundaries, days use UTC midnight, weeks start Monday, and months/years use calendar boundaries. Multi-period groups share epoch-based anchors. Partial first/current bars are retained; source gaps do not produce synthetic bars. Misaligned source bars are rejected rather than split. Exchange-session/trading-day timezone aggregation is not implemented. Aggregates feed study calculation and BarStudy publication without inserting extra aggregate DataSet/PriceData rows.

Publication uses SqlBulkCopy into a temporary table followed by changed-only updates and missing-row inserts under one serializable transaction. It revalidates SQL study/source metadata and rereads/hashes raw source prices under range locks before writing. Changed inputs/configuration discard the generation for retry. It never deletes historical study rows. Empty sources or historical outputs absent from the rebuilt target timeline block publication. Publication verifies study OHLCV against aggregated inputs, while concurrency validation uses the original source-price fingerprint. SQL configuration/loading has been verified in a live dry run; publication writes remain unverified against that database.

Price changes are detected by reading/hash-checking the full source dataset each poll. A fresh collection replays **all supplied source history** on each change and on restart. This deliberately avoids assumptions about plugin rollback limits, warm-up sufficiency, or recoverability from published outputs. It can be expensive for large histories and can fail to publish if prices keep changing faster than replay completes. It is a correctness baseline, not the final low-latency engine. Only one calculation runs at a time; one monitor's failure is logged and other monitors continue. Configure one host as the owner of each StudyCol.

Logs are in `%LOCALAPPDATA%/FIGMarketLive/marketlive.log`; the current implementation does not rotate them. The tray dashboard shows calculation status/errors. Plugins are trusted application code, run in process; reload DLL builds by restarting the host.

## Binary format: FIGS version 1

BarStudy.Studies is `varbinary(max)`. Bytes represent published values, never collection objects, CLR memory or checkpoints. The initial self-describing format repeats names per row to handle sparse/dynamic outputs correctly; schema-ID/name-deduplication is deferred until profiling justifies it.

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

1. Disposable SQL integration tests for transactional publication, stale-source rejection and concurrent writers.
2. Real collection/configuration parity and replay performance measurements.
3. Continuous OHLCV acquisition into SQL, catch-up coverage and pacing. Embedded IBKR connection, contract resolution, historical and snapshot operations are implemented behind IMarketDataAdapter; the configuration dialog uses connection and lookup operations.
4. Full database bootstrap/migrations beyond the additive provider/settings migrations.
5. Shared eSignal import and exchange-session aggregation rules (UTC aggregation is implemented).
6. Optimized recent-bar replay, only after parity tests establish collection rollback limits.
7. Revisioned chart IPC, native decoder, chart launch integration and live workspaces.

No trading/signal execution is invoked. FIGSignalSvc StudyHistory and its JSON repositories are unchanged.

## Validation performed

Release build of FIGMarketLive.slnx succeeded with zero warnings/errors. The regression runner passed 202 database-free checks, including precision/type round trips, every truncation point of a sample payload, deterministic encoding, correction/restart replay and actual managed DLL loading via a fixture plugin.

An optional real-plugin test accepts an output folder, a plugin directory containing exactly one manifest, and a parameter JSON file:

```powershell
dotnet run --project tests/FIGMarketLive.Regression -c Release --no-build -- .test-temp/marketlive PATH_TO_PLUGIN_DIRECTORY tests/FIGMarketLive.Regression/default-parameters.json
```

The existing `FIGSignalCollection/FIG.StudyCollections.LADX/publish` DLL passed this test using `{}` defaults: 204 total checks for the initial implementation, including sequential-vs-batch parity and binary round trips for its actual outputs. Its adjacent source-tree `ladx-parameters.json` was rejected by that published DLL for unknown setting `adxStrongThreshold`; matching plugin builds and parameter versions are required. No plugin DLL or parameter file was changed. These tests use synthetic bars and do not establish production performance or parity with an external feed. SQL settings migrations and monitor-loading dry runs have been verified; publication writes and live broker data retrieval remain unverified. Additional adapter, aggregation and configuration checks are described in CONFIGURATION.md.
