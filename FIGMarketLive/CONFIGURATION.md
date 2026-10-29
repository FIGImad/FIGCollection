# Configuration

The Configuration button and tray menu open the same dialog. Opening configuration pauses the study worker; restart it explicitly after saving. Local settings retain plugin path and polling preferences; monitor definitions live in SQL.

## Database

Enter or update the SQL Server connection string, including Database / Initial Catalog. **Test database and schema** opens the target database, checks visibility of its required columns, SELECT permissions, price types, binary BarStudy.Studies storage and dataset/price/study composite unique indexes. Failure to open the target triggers a best-effort master catalogue check to distinguish inaccessible from missing/not-visible databases. Lack of visibility is never reported as proof that the database does not exist.

Results list schema problems. Configuration creates the additive TickerProvider table when missing and migrates legacy provider settings as described below; it does not create databases or migrate text study history. Other tables and interval seed rows must already be present. Schema details such as trigger correctness, every foreign key, all defaults and full write permissions are not exhaustively certified. SQL writes report their own permission failures. The optional Event notification table is not required by this polling implementation.

## Market-data adapter

Select the provider. The installed provider is **IBKR — TWS / IB Gateway**, hosted directly inside FIGMarketLive using IBKRProvider.dll. FIGBrokerSvc, its HTTP lookup endpoints and FIG authentication are not used by this workflow.

The dialog generates fields from the adapter's descriptor:

- Host: defaults to 127.0.0.1.
- API socket port: starting value 7497; match the port configured in your TWS/Gateway instance.
- Dedicated API client ID: starting value 31; use a positive ID not in use by another application on the same instance.
- API handshake timeout: starting value 15 seconds, after the SDK socket connection call returns.
- Request timeout: starting value 30 seconds for provider requests.

Log in to TWS or IB Gateway separately and enable its socket API access. FIGMarketLive does not accept your brokerage login password. **Connect / test** constructs the adapter, connects the socket and waits for the provider connection acknowledgment. It does not certify market-data entitlements: actual data requests can still fail. **Disconnect** drains active work before releasing the provider.

Changing settings invalidates cached test results but leaves ticker actions available. Search and dataset actions validate the current database and connect/reconnect the adapter on demand, without requiring manual test-button confirmations. Failures identify the Database or Market data adapter tab and the relevant test button. Closing or saving the dialog disconnects its configuration session. This session is for lookup/setup; continuous SQL price acquisition is not wired into the study worker yet. TWS socket establishment can take longer than the handshake timeout because the underlying synchronous SDK connect has its own socket behavior. Cancellation waits for active SDK work to drain rather than allowing overlapping calls or disposal while that work is running.

The Connect / test button has a result icon beside it: succes.png after a successful connection, fail.png on validation/connection failure. Retesting, changing adapter settings or disconnecting clears the previous result. Errors include the configured endpoint/client ID and the underlying SDK socket/error message when available. A refused connection means the endpoint did not accept the socket at that time; check that the adapter port matches the running TWS/Gateway instance rather than assuming the starting port is correct.

Stock search uses provider SymbolLookup; selection is resolved through ContractInfo before saving to SQL. Futures search uses ContractInfo for an explicit YYYYMM contract month or YYYYMMDD expiry. The current provider interface returns one resolved futures contract, not a chain of all expiries. Include exchange and currency to disambiguate.

Existing configuration files remain readable. The retired Broker URL/account/token section is ignored and removed when saving. Existing monitor definitions are preserved. Legacy broker-backed dataset selections should be reselected through the direct adapter so provider-neutral instrument metadata and provider identity are recorded. Connection strings remain local JSON; prefer integrated authentication.

## Provider-neutral extension contract

`FIGMarketLive.Core/MarketData/IMarketDataAdapter.cs` defines connection/disconnection, instrument search/resolution, historical OHLCV and last-price snapshots. Its public contracts contain no IBKR SDK types, HTTP credentials or trading operations. Provider instrument IDs are strings; bar volume and multipliers retain decimal precision at this boundary. Existing SQL/model limits are checked separately when saving.

`IMarketDataAdapterFactory` supplies the descriptor, parameter defaults, validation and instance construction. `MarketDataAdapterCatalog` selects factories by ProviderId. A future adapter implements these interfaces and is registered in the application composition root; the database, study code and configuration form do not need provider-specific branches. Field generation and help text come from the selected descriptor. Dynamic loading of adapter assemblies is not implemented.

The IBKR implementation lives in FIGMarketLive.Adapters.IBKR. It owns SDK mapping, callback/connection infrastructure and serialized background execution. Snapshot results are prices, not complete OHLCV bars; use GetHistoricalBarsAsync for authoritative bars. Requests are serialized, but a provider-specific historical pacing scheduler remains future work. No trading or position-tracking services are started.

## Ticker and DataSet

The third tab always offers search and dataset setup; actions are temporarily disabled only while an operation is running. Compact search fields accept the symbol, FUT/STK type, exchange, currency and expiry. Select an instrument and an interval loaded from dbo.Interval, then use **Save Ticker** beside Interval to create/reuse its ticker and dataset. The Market data adapter tab uses Branding/plug.png.

**Local database symbol** is the local unique key. Futures default to the resolved local contract symbol so different expiries can coexist under the existing unique Ticker.Symbol constraint. Shared contract metadata remains in Ticker; provider symbol and instrument ID are saved in TickerProvider. Provider IDs are never reused as local Ticker or DataSet identity values.

**Save Ticker** re-resolves the selected contract, verifies its ID when available, validates its minimum tick and integer multiplier, then transactionally inserts/reuses Ticker, DataSet and TickerProvider. A conflicting local symbol is rejected; existing shared contract metadata is not overwritten. Database writes happen when this button is clicked, even if the dialog is subsequently closed without saving configuration. **Save configuration** saves connection/adapter/application settings, not dataset selections. Draft settings can be saved without passing tests; search and SQL setup validate connections on demand.

Configured price datasets appear in a multi-column grid with dataset ID, symbol, contract, security type, interval, StudyCol count and enabled SQL monitor count. Configuration loads this list from SQL when opened, and refreshes it after successful database checks and ticker saves. There is no JSON dataset fallback. If SQL cannot be accessed, the database test reports the problem and the list remains empty. There is no separate Load existing DataSets button or dataset dropdown.

### Delete a dataset

Select a row and use **Delete selected DataSet**. The application previews the actual SQL dataset and every StudyCol with its ticker/interval (StudyCol has no DataSetId in this schema). Confirmation identifies the dataset, price/study row counts, and warns that all dependent StudyCols and configured monitors will be removed. No is the default response.

The first SQL refresh preserves selection by DataSetId, even if row order or display labels change. Before confirmation, the selected dataset is validated by DataSetId, TickerId and IntervalId rather than broker/root symbol text. Confirmation uses the current SQL names. If the selected dataset disappears, selection is cleared instead of moving the delete action to another row.

After confirmation, a serializable transaction rereads and locks the dataset and StudyCol dependencies. Changed identity or StudyCol IDs cancel deletion and require a fresh confirmation. It deletes dependent BarStudy rows, the matching StudyCol rows, PriceData for the selected dataset, and the DataSet row. Ticker metadata is retained. Unrecognized cascading relationships are rejected; other foreign-key failures roll back the transaction rather than causing a partial deletion. Shared databases with additional dependent tables may therefore require explicit integration before deletion is possible.

Removing the dependent SQL StudyCol rows also removes their runtime monitors; there are no local monitor definitions to clean up. Ticker and TickerProvider are retained because deleting an interval dataset does not delete its instrument. Closing the dialog does not undo deletion. If the database connection was edited, save/reopen configuration before deletion so identities cannot refer to a different configured database.

## TickerProvider and legacy settings migration

TickerProvider has a primary key on (TickerId, ProviderType), a foreign key to Ticker, a string ProviderInstrumentId (IBKR ConId or another provider's ID), and ProviderSymbol (for example NQ while the local ticker is NQZ6). Exchange, currency, expiry, minimum tick and multiplier remain in Ticker. DataSet retains ticker/interval associations. The SQL-backed reader reconstructs a provider-neutral instrument from these tables.

On database testing/opening Configuration, the app creates TickerProvider if absent. When legacy DataSelections exists, it validates each entry against its SQL dataset identity, deduplicates provider metadata by ticker/provider, and commits all mappings together. Conflicting metadata, unknown providers or missing parent identities stop migration without removing JSON. Existing mappings may be enriched with previously unknown IDs but are not silently replaced during migration.

Only after SQL commit does the app remove DataSelections from the active settings file. A `marketlive.json.before-ticker-provider-*.bak` backup retains the original. The app checks for concurrent file changes and keeps legacy JSON on migration/save failure so rerunning is safe. Do not commit settings backups; they may contain the same connection credentials as the original. New settings files omit DataSelections completely. Saving a different database connection is blocked until nonempty legacy entries have migrated to their original database.

For explicit migration without opening Configuration:

```powershell
dotnet FIGMarketLive/bin/Release/net10.0-windows/FIGMarketLive.dll --migrate-provider-settings PATH_TO_MARKETLIVE_JSON
```

Creating the new table requires suitable SQL schema permissions. Database/CreateTickerProvider.sql is available for manual installation. No existing Ticker/DataSet rows or price/study data are deleted by migration.

Every enabled SQL StudyCol becomes a runtime monitor. Dataset selection is automatic using the same compatibility rules as the editor. Broker price acquisition remains a later integration step.

## Market Data & Studies

This tab uses bar-chart.png and edits dbo.StudyCol directly. On first opening it loads the current SQL catalog. **Reload** refreshes later database or ColType changes; switching tabs does not intentionally discard the editor draft. **New** starts a new row; selecting an existing row loads its values for update.

- Ticker contains only SQL tickers with at least one configured DataSet.
- Interval contains direct datasets and intervals that can be aggregated without splitting source bars. A five-minute dataset supports ten/fifteen/twenty/thirty minutes, hours divisible by five minutes, and calendar days. A larger source cannot create smaller bars or incompatible multiples.
- An exact target dataset is preferred; otherwise the closest compatible source is chosen deterministically. The editor shows that source dataset and whether aggregation is required. Month/week/year compatibility is evaluated as calendar units rather than the schema's approximate number of seconds per month/year.
- ColType comes from dbo.ColType. ColParams is a multiline JSON object editor with a format/validate action; malformed or case-ambiguous duplicate parameters are rejected.
- Enabled is an editable checkbox. OrderSeq is hidden and every save writes 1.

**Save StudyCol** validates dataset compatibility and ColType again inside its SQL transaction. It writes immediately; the main Save configuration button is for local settings. The running worker reloads enabled studies and their source datasets each polling pass, with runtime UTC aggregation for compatible intervals. Existing DLLs still use the FIGSignalSvc plugin package/manifest loader, and ColType.DLLName must match the loaded plugin. Historical outputs are not automatically deleted if a new configuration changes the output timeline.

## SQL-driven runtime monitors

Monitors is no longer an active JSON setting. Startup/configuration validates legacy StudyCol references and SQL source compatibility, backs up the file as `marketlive.json.before-sql-monitors-*.bak`, then removes the old section. Missing/incompatible definitions retain the legacy JSON for correction. The normal runtime uses all enabled SQL studies, independent of legacy settings; disabled/deleted studies stop on the next pass. Changes to source choice, target interval, ticker metadata, DLL assignment or JSON parameters invalidate the study cache and cause a fresh replay. Other valid monitors continue if one definition is invalid. No enabled studies produces an attention message while the worker waits for configuration.

Exact interval datasets are preferred. Otherwise the closest/largest compatible source is selected, with the lowest dataset ID breaking ties. Only existing source prices are aggregated: OHLC is first/max/min/last and volume is summed with overflow checking. Partial first/current target bars are retained. No artificial bars are inserted across gaps. Source bars must fit wholly within target boundaries.

Aggregation is explicitly UTC-based, not exchange-session based: intraday periods are anchored at Unix epoch, daily periods at UTC midnight, weeks on Monday, and months/years on real calendar boundaries. Multi-period day/week/month/year groups use consistent epoch-based anchors. A futures exchange trading day can differ from a UTC day; session/timezone aggregation remains future work. Aggregates are calculation inputs and BarStudy output rows; they do not create duplicate PriceData datasets.

Publication rereads raw source prices and SQL configuration under the transaction before accepting results. It validates output coverage against the aggregated timeline rather than comparing target bars to every raw source bar. New source corrections and forming-bar updates therefore invalidate/rebuild the affected generation without checkpoint persistence.

Explicit legacy cleanup:

```powershell
dotnet FIGMarketLive/bin/Release/net10.0-windows/FIGMarketLive.dll --migrate-sql-monitors PATH_TO_MARKETLIVE_JSON
```

## ColType management

The ColType tab lists Type, DLLName and Description and supports New, Save and Remove. Type and DLLName are required; DLLName must end in .dll. Fields honor the supplied schema's length limits. Updating a row can change DLLName and Description; a name may be changed only when no StudyCol references it. Removal requires confirmation and is refused for a type in use. Changes are committed immediately to SQL. DLLName is stored metadata, not an instruction to download, copy or execute a DLL. After changing types, use Reload in Market Data & Studies to refresh its dropdown.

Dataset dependency previews and deletion now use the same interval/source-selection rules, including studies aggregated from the selected source dataset. The dependency list is still rechecked after confirmation inside the deletion transaction.

## Checks

The regression runner includes embedded-adapter checks with a fake SDK boundary: settings validation, handshake, metadata conversion, futures expiry, selected-contract identity, historical request mapping, serialization during cancellation and idempotent disposal. An alternate provider fixture uses the same factory/catalog/interface. No live provider connections are made.

```powershell
dotnet build FIGMarketLive.slnx -c Release -m:1
dotnet run --project tests/FIGMarketLive.Regression -c Release --no-build
dotnet FIGMarketLive/bin/Release/net10.0-windows/FIGMarketLive.dll --configuration-self-test .test-temp/marketlive-ui
```

The Windows UI self-test renders all three tabs off-screen, saves a test configuration, and checks that existing monitors/selections and the IBKR parameters survive. It writes PNG previews and result.txt into the specified test folder without opening SQL or provider connections.

## Branding and tray status

The executable and both windows use Branding/FIGMonitor.ico. PNG/ICO assets are embedded in the assembly, so source-folder paths are not required at runtime. Configuration, Start studies and Pause buttons (and their tray menu entries) use settings.png, play.png and pause.png. The Database and Ticker/DataSet tab headings use database.png and chart.png.

The tray uses FIGMonitorOff.png while idle, paused or connecting; FIGMonitorOn.png only while monitoring is active and its runtime adapter is connected; FIGMonitorWarning.png takes priority for study/database errors, deferred publication or a lost adapter connection. Warnings are retained per StudyCol and cleared when that collection succeeds; another collection's success does not clear them. Pause resets the attention state. The tray tooltip also describes the state.

Start studies now establishes a runtime adapter connection before running the SQL study worker. Pause shuts it down. This connection supports accurate tray connection status; automatic price acquisition into SQL remains pending. The configuration dialog continues to own a separate, temporary setup connection while monitoring is paused.
