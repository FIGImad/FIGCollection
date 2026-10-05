# FIGSignalExSvc study plugins

`FIGSignalExSvc` resolves `StudyCol.ColType` through collection plugins loaded from the configured `SingalProcessing:PluginPath` directory. Absolute paths are used directly; relative paths are resolved against the service executable directory. If omitted, the legacy `StudyPlugins:Path` setting is used, then `Plugins` as the default. Restart the service after changing this setting.

## Included plugin

The build copies the ADXTrend package to:

```text
Plugins/ADXTrend/
  plugin.json
  FIG.StudyCollections.ADXTrend.dll
```

The database value `ColType = ADXTrend` maps to `ADXTrendFactory.ColType`; the database does not contain a DLL or class name.

## Adding a collection

1. Create a class library targeting the same framework as the host.
2. Reference `FIG.Studies.Core` and, when needed, `FIG.Studies.Common`.
3. Implement exactly one `IStudyCollectionFactory`.
4. Add a `plugin.json` containing `colType`, `assembly`, `apiVersion`, and `version`.
5. Deploy the manifest and collection DLL into their own plugin folder.
6. Restart `FIGSignalExSvc`, then enable the matching `StudyCol` row.

Do not deploy private copies of `FIG.Studies.Core`, `FIG.Studies.Common`, or `FIGCommon` inside plugin folders. They are shared by the host.

Use the following command to verify discovery without starting price processing:

```powershell
dotnet FIGSignalExSvc.dll --list-plugins
```

Replacing an active plugin requires a service restart. Hot replacement is intentionally not supported because collection instances contain live replay and smoothing state.

## Persistent calculation checkpoints

The shared MA, ADX, BB, SEM, WMA, ROC and Extended studies support versioned calculation checkpoints. These preserve per-bar parameter histories (including smoothing seeds), current outputs, and the collection's price/output buffers. Study history is saved on every bar; by default checkpoints are captured and serialized every 100 completed study bars during catch-up, and every 30 during live processing. A following study bar confirms completion, so a scheduled snapshot of the last/forming bar is deferred until that successor arrives. Each checkpoint also includes recent state for the existing three-bar correction replay. Repeated updates to a bar do not advance the checkpoint count, and market gaps do not count as bars.

Checkpointing is disabled by default. Deployment requires:

1. Update the shared assemblies and implement checkpoint support in **every enabled collection plugin**, as described below. An unsupported collection is rejected when checkpoint mode is enabled; it cannot silently resume from partial state.
2. With FIGSignalSvc stopped, run `FIGCommon/DataSchema/study_history_checkpoints.sql` against its calculation database (the private database in remote publication mode), including when upgrading the previous single-checkpoint design. This adds nullable `CheckpointState` and `CheckpointSavedAt` columns to `StudyHistory` and replaces the unique index on `StudyColId` with a filtered unique index on `(StudyColId, RawTime)`. Existing payloads are preserved. If the earlier `StudyCheckpoint` table exists, the migration transfers its payloads to matching rows and removes the old table in the same transaction. Unmatched, duplicate or conflicting legacy rows abort without discarding checkpoints. The script also supports fresh installations and repeated runs. The service does not run migrations automatically.
3. Run `FIGCommon/DataSchema/study_signal_progress.sql` once with all signal generators stopped. The durable signal-processing marker is required by the updated service, including when checkpoints are disabled. See the upgrade behavior below.
4. Set `SingalProcessing:Checkpoints:Enabled` to `true` in the effective service settings, including any development override, and restart the service.

Processing streams aggregated prices and commits bounded batches while replay is still running. Each batch atomically saves its study history, signals, durable signal progress, and any scheduled checkpoints; batches without checkpoints still commit history normally. Defaults are 250 study bars or 16 MiB of checkpoint text (UTF-16 bytes), whichever comes first; an oversized individual checkpoint is committed alone. A failed batch discards in-memory state, restores the latest durable checkpoint, and recalculates the later committed rows and any new bars on retry. Pending checkpoints stay in bounded memory; no whole-replay temporary file is written.

The bulk writer stages history and optional checkpoints together using `SqlBulkCopy`, then performs one set-based update for existing rows and one insert for missing rows in the caller's transaction. Row existence is checked in the database, so gaps inside the replay window are repaired even when their timestamps precede the latest stored bar. Scheduled checkpoints are present when history triggers run; other rows have both checkpoint columns set to NULL. Recalculated rows outside the cadence also clear any old checkpoint on those rows. Collection locking and row validation happen once per batch, and history triggers run per statement rather than per checkpoint. A changed history tail aborts the batch with the collection ID and expected/actual timestamps; ambiguous duplicate rows report the affected timestamp. Older snapshots outside the replay window remain untouched. The previous single-checkpoint index is detected before replay and reported as a migration error. The checkpoint columns do not change when selecting a new interval. The signal replay guard below has a separate required migration.

Optional settings under `SingalProcessing:Checkpoints` are `EveryNBars` (live interval, default 30), `CatchUpEveryNBars` (default 100), `LiveMaxBars` (default 10), `BatchSize` (default 250), `MaxBatchBytes` (default 16777216), and `PricePageSize` (default 2048 one-minute prices). All values must be positive. A processing cycle with more than `LiveMaxBars` aggregated study bars after the correction window uses the catch-up interval; cycles with 0–10 use the live interval. Post-checkpoint recovery rows count as work even when already present in StudyHistory. Selection reads only enough bars to distinguish the two cases, retains that interval for the whole cycle, and does not count the full price history or buffer it in memory. It is independent of database transaction batch size: the final short batch of a large rebuild still uses 100. A later backlog automatically selects catch-up again.

Switching intervals preserves the most recent checkpoint anchor and bars already counted. If the live interval is already overdue, the next eligible completed bar gets a checkpoint; otherwise the next checkpoint falls 30 bars after the previous one. Existing checkpoints inside the correction window are preserved when replayed. Set both `EveryNBars` and `CatchUpEveryNBars` to the same value for a fixed cadence, or both to 1 for a checkpoint on every completed bar. These settings do not change the payload format or require a new SQL migration. Restart the service after updating configuration. Aggregates crossing price-page boundaries are held until complete, so tuning page size does not change results. Logs show the selected interval per cycle, and each committed batch logs its last timestamp, checkpoint count/bytes, and database-write duration. Calculation/warmup progress is logged approximately every ten seconds.

On restart, the host selects both the latest study-history rows and the newest remaining checkpoint by `RawTime`. The checkpoint may precede the history tail. It restores that state, starts at the checkpoint's three-bar correction window, and regenerates every subsequent study-history row from source prices through the latest price. Existing rows are updated, missing/new rows are inserted, and the selected cadence continues from the restored checkpoint. Only completed bars newer than the independently stored signal-processing marker may change signals. Replayed bars at or before that marker cannot create, close or reverse signals, even when recalculated outputs differ. Normally recovery replays up to 100 post-checkpoint bars from catch-up or 30 from live processing, plus the correction window and any new prices. Source prices must cover the correction window and reach the committed history tail; otherwise recovery fails explicitly. Identity IDs are not used to infer bar order, since bulk inserts and gap repairs can assign IDs out of chronological order. Database errors fail processing rather than disabling persistence.

A missing/incompatible checkpoint triggers replay from **all available one-minute prices**, with aggregate intervals preserved across page boundaries. It does not use `GetMaxLen()` as a warm-up approximation. Prices must cover the desired initialization history; checkpointing cannot recover deleted historical inputs. Parameter changes, collection/study assembly build changes, and state schema changes invalidate saved state. Rebuilds refresh the recent history and new rows; they do not retroactively rewrite all historical signals or outputs.

By default checkpoints keep the full retained collection price/output buffers for correctness; payload size and SQL write cost grow with those buffers. Plugins that own all their calculation history can explicitly omit redundant host history through the audited hooks described below. LADX uses this compact format and reconstructs its revision snapshots from one anchor state plus exact subsequent inputs. Classic instead references its rolling price window in `StudyHistory`, eliminating the embedded window and repeated prices inside study parameters. Restoration reads OHLCV/time columns once for the same collection and referenced range, validates its count, order, boundaries and checksum, then keeps prices in memory. Missing or changed rows reject restoration and trigger replay from source prices; SQL connection/query errors still fail processing. Payloads remain ordinary JSON in `CheckpointState`, readable from SSMS. Classic requires preceding history rows to restore a checkpoint. Catch-up uses approximately 70% fewer checkpoint captures than a fixed 30-bar interval; every-bar prices/study results are still written. Ordinary history reads omit the checkpoint payload. Run one signal generator per StudyCol.

To rebuild the latest 100 bars, stop the service and remove that collection's 100 newest `StudyHistory` rows. After restart, the latest compatible surviving checkpoint is restored even if it is older than the remaining history tail. Calculation regenerates the intervening retained rows and then rebuilds the removed bars from price history. A running worker also discards cached calculation state if the history tail changes, but manual history maintenance should be performed with the service stopped. This restores study calculations; it does not undo already generated signals, published events, or trades. Correcting older price inputs requires deleting/rebuilding history from the first affected bar onward, since later checkpoints incorporate those inputs. Removing isolated middle rows is not detected as a tail rewind.

The migration does not fabricate checkpoints for older rows that have none. Only scheduled new/recalculated rows acquire them. Existing compatible per-bar checkpoints can be restored as the new cadence anchor. Older snapshots are not purged retroactively. If no usable checkpoint remains, including a restart before the first 30 completed bars, the host rebuilds from available price history. Rebuilds retain the existing policy of updating recent rows and inserting new ones; backfilling checkpoints on older existing rows requires a deliberate full history rebuild.

### Signal replay protection

`dbo.StudySignalProgress` stores one `ProcessedThrough` timestamp per collection, independently of study history and checkpoint payloads. Completed bars advance this marker even when they emit no signal. A forming bar does not advance it, so that bar can emit its first signal after it closes, including across a restart. Deleting/rebuilding study-history rows never rewinds signal progress. Do not clear this marker as part of calculation-history maintenance.

Signal decisions begin by locking the collection row (`UPDLOCK,HOLDLOCK`) inside the same transaction used for history, signal rows, signal progress, and checkpoints. Each attempt reloads progress and the latest signal records under that lock; signal state is not retained after a failed or uncertain commit. Two workers cannot both decide to generate a signal for the same completed bar. A successful commit followed by a lost response is safe to replay: its persisted marker excludes the already processed bar. Database-triggered publication messages participate in the transaction, and the publication wakeup happens after commit.

The latest signal's start/stop timestamp provides an additional inclusive replay guard, including when that signal is already closed. Only actual signal changes are written; replay does not resave closed signals or issue new tags for them. Strategies are restricted to those assigned to this collection.

**One-time upgrade:** stop all signal generators, run `FIGCommon/DataSchema/study_signal_progress.sql` in the calculation database, deploy/rebuild the updated service, then restart. The migration initializes each new marker from the maximum of existing history times and existing assigned signals' start/stop times. It treats all existing history as consumed to avoid issuing historical trades during the upgrade. This conservative initialization also skips any currently forming history-tail bar once; normal forming-bar handling resumes on new bars. Re-running the migration leaves existing markers unchanged. The service refuses to write history or signals if this migration is missing. No live database is migrated automatically.

### Collection plugin contract

`StudyColBase` defaults to unsupported. A collection can override `CheckpointSchema` with a stable version string **only after auditing all mutable collection state**. Collections with additional fields must implement `CaptureCollectionState()` and `RestoreCollectionState(JsonElement)` and include that state. All contained studies must support checkpoints too. Unmodified external plugins continue to use the legacy path while checkpointing is disabled.

```csharp
// Only sufficient when the collection has no mutable state outside the base class and its studies.
protected override string? CheckpointSchema => "my-collection-v1";
```

Custom studies must opt in through `BaseStudy.CheckpointConfiguration`, covering every calculation setting; implement the runtime hooks for mutable values outside `BaseParams`. Parameter types must round-trip through `StudyStateJson.Options`, which includes public fields. Use `BaseParams.OnCheckpointRestored()` to reconstruct derived/private values. Unknown subclasses of the supported shared studies are deliberately not opted in. Restore into a fresh collection and discard it on any restore failure. Collection output buffers currently support null, decimal, double, float, int, long, bool and string; unsupported output types fail capture explicitly.

`CheckpointNeedsHistoricalOutputs` defaults to `true`, and `CheckpointPriceHistoryLimit` defaults to the collection's full price capacity. Override these only after verifying that omitted history is not needed by any study, including during correction replay. The latest output and bar timestamps remain in checkpoints, and at least one input price is required. LADX overrides both because its engine is self-contained. Classic keeps historical outputs and references the longest configured rolling price window plus ten prices for corrections; its custom parameter histories and support/resistance runtime fields also support restoration. Other collections retain the defaults unless they explicitly override them. These choices are included in the checkpoint identity. This shared-core/plugin build update invalidates older checkpoints, causing one full historical warmup before new compatible state is available.

`CheckpointPricesFromHistory` defaults to `false`. Classic and L2 opt in because their price-buffer calculations use only OHLCV/time. Opted-in collections use checkpoint format 2: `Prices` is empty, `PriceWindow` identifies the required rows and checksum, and study parameters reference their retained bar prices by timestamp. The host calls `GetRequiredCheckpointPrices` to validate the request before querying, then `RestoreCheckpoint(checkpoint, historyPrices)` to validate the inputs and hydrate state. The ten retained output-bar prices are still stored with their original metadata. PriceData database IDs and decimal trailing-zero scale are deliberately excluded from the checksum. The checksum uses timestamped SHA-256 row digests and incremental XOR prefixes, with row count/boundary/order validation, to avoid rescanning the entire window on each capture. Historical rows must remain available for fast restoration, including required warmup inputs; fallback replay does not backfill all older study-history rows.

L2 preserves all 15 custom study types, including the six runtime fields outside the BaseL1/L2 parameter histories. It keeps recent outputs and references `max(MaxPeriod, 24564) + 10` prices for its longest rolling cycle and correction replay. It uses the same host cadence, restart recovery and signal replay protection as the other checkpoint-capable collections.

ADXTrend and L8L6 also support format-2 checkpoints and the same host cadence/replay protection. ADXTrend preserves the active strategy's side/bar counter and seven decimal indicator caches; L8L6 preserves its strategy side history. Both retain ten recent output bars and reference their longest required input-price window plus ten prices. ADXTrend's optional, commented-out StudySlopeTrend remains unsupported until its separate buffer is made rewind-safe.

### Verification

```powershell
dotnet run --project tests/FIGStudyCheckpoint.Regression
dotnet run --project tests/FIGStudyCheckpoint.Regression -- --sql
dotnet run --project tests/FIGSignalReplay.Regression
dotnet run --project tests/FIGSignalReplay.Regression -- --sql
dotnet run --project tests/FIGL2Checkpoint.Regression -c Release
dotnet run --project tests/FIGADXTrendL8L6Checkpoint.Regression -c Release
dotnet run --project tests/FIGL2ManualCheckpoint.Regression -c Release
```

The regression suites compare uninterrupted processing with JSON checkpoint restoration during warm-up, steady state and recent-bar corrections for all seven shared studies, Classic, and LADX. Coverage includes sparse snapshots, restarts between checkpoints, repeated forming bars, regeneration of existing post-checkpoint history, deletion of 100 bars, count/byte batch limits, and interrupted-batch recovery. The shared suite also verifies that capture work only occurs at the configured interval. The optional SQL checks create and remove a uniquely named LocalDB database and verify migration upgrades/repeatability, sparse and per-bar bulk writes, trigger counts, rollback, corrections, gap repairs, out-of-order identity IDs, changed-tail/duplicate diagnostics, and restart from a checkpoint older than the history tail. They never use the service's production connection string or submit trades.

The signal replay suite separately exercises starts, stops, direction changes, flat bars, changed replay outputs, restart boundaries, forming bars, rollback/retry, lost-acknowledgement retries and strategy isolation. Its optional SQL suite uses the actual `StudySignal` orchestration, authoritative schema, history writer and publication trigger in a disposable database. It checks that replay/deletion repair produces no extra signal IDs or publication messages, including concurrent workers, and that progress rolls back with signal/history writes.
