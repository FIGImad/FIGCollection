# FIGCharts

Native Windows C++ financial chart viewer using the sibling FIGSignalCpp ChartDirector installation. Supports CSV, FIGDATA binary reports, and SQL Server study history.

## Build and run

Open `FIGCharts.slnx` in Visual Studio 2026 with Desktop development with C++ installed. Build **Release / x64** and start FIGCharts. The executable is `bin/x64/Release/FIGCharts.exe`; the build copies `chartdir70.dll` beside it. The project uses toolset v145, matching FIGSignalCpp. Override the MSBuild `ChartDirectorDir` property to use another ChartDirector installation.

Or run `Build.ps1`, then `bin/x64/Release/FIGCharts.exe`.

## Load a backtest

Use **Open data**, drag a CSV or FIGDATA file onto the window, or pass its path on the command line. Open a backtest output directory's `prices.csv` for OHLC/volume. Open a FIGSignalCpp study-report CSV to also inspect the exported numeric study columns. Generate these with FIGSignalCpp's `Run-StudyReport.ps1` using the same dataset, interval, collection and parameters as the backtest.

The backtest's `results.csv`, candidate `*-trades.csv`, and `*-monthly.csv` are summaries rather than OHLC series and are not supported as chart inputs. This first version does not join trades onto price bars or calculate FIG collection studies from parameter files.

- OHLC bars are the default. Use **Chart type** to switch between OHLC, candlesticks and a close-price line. **Show volume** toggles the volume bars.
- Move over the price chart or a study panel for a crosshair: the vertical line follows the same bar across panels, and the horizontal line shows the active panel's axis value. Time labels use the selected display zone, with a UTC offset in the crosshair and legend. Drag scrolling remains available.
- **Go to...** opens a date/time picker in the selected display zone and centers the requested bar where possible. Times in gaps select the next available bar; times outside the loaded report range show an explanation.
- The separate, resizable **Legend** window shows OHLC, visible volume and plotted study values for the hovered bar, or the last visible bar when the pointer leaves the chart. Missing values display as `--`. Close it to hide it; the **Legend** button reopens it.
- Built-in price indicators have been removed; studies come directly from the report.
- Select an available study, then click **Add overlay** or **Add text** to add it permanently to the legend. Selection alone never adds a study; both buttons are disabled until a study is selected. Adding another study retains all previous entries; adding the same study twice does not duplicate it. There is no four-study limit.
- Select a text study in the legend to inspect it in the single panel below the chart. Selecting another text study switches that panel. Selecting an overlaid study closes the inspection panel without removing any study.
- Edit studies directly in their legend rows: **Display** has a Text checkbox (checked = text, unchecked = price overlay), **Color** opens a color picker, **Line** offers solid/dashed/dotted/dash-dot, and **Thickness** offers 1–6 pt. Click a line or thickness cell to open its dropdown. Settings apply to overlays and the inspection panel. The trash icon deletes that row.
- Added studies and styles are saved by exact study name in `%LOCALAPPDATA%/FIGCharts/studies.txt` and restored when another report has matching names or the app restarts. Studies absent from a report remain saved for future reports. The inspected panel is not restored automatically. Multiple running instances share these preferences; the last saved settings win.
- Drag left or right within the price chart or a study panel to pan through history. Drag right to see earlier bars, or left to see later bars. Zoom changes the number of visible bars; the horizontal scrollbar also moves through history. **Latest** jumps to the end.
- **Export PNG** saves the current chart and selected studies.

CSV headers are case-sensitive: `RawTime,Open,High,Low,Close` are required; `Volume` is optional. `RawTime` is Unix seconds, strictly ascending, for one dataset. `Id` and `DataSetId` are metadata. Extra columns containing numeric values become studies. Blank study fields remain gaps; text columns and entirely empty columns are omitted. UTF-8 BOM, quoted fields, embedded commas/newlines and escaped quotes are supported. Invalid OHLC, invalid numbers, duplicate timestamps and malformed rows are rejected with an error. Loading is synchronous; large files can temporarily pause the interface. Rendering reads only visible bars and uses exported study values without recalculating indicators.

## Validation and extension points

`FIGCharts.exe --self-test` exits with code 0 when parser regression tests pass. `FIGCharts.exe --render input.csv output.png` renders the latest 200 bars with the first available numeric study in the lower panel, without opening a window; exit code 1 indicates failure.

`FIGCharts.exe --ui-test input.csv preview.png` runs native control interaction tests in a hidden window, using a fixture with at least 600 bars. It checks chart selection, volume visibility, crosshair/legend alignment, persistent study additions and style settings, overlay/panel changes, removal, date navigation and drag panning, and saves crosshair and dark-theme previews. It also checks settings persistence, daylight-saving conversion, workspace save/load and the per-row trash action. Failures exit with code 1 and write `preview.png.txt`.

- `src/CsvData.*`: CSV parsing, validation and data model.
- `src/ChartView.h`: ChartDirector chart construction and display options.
- `src/StudyUi.h`: legend, study settings, and preference persistence.
- `src/main.cpp`: native Windows controls, file dialogs, navigation and PNG export.

ChartDirector remains a separately licensed dependency. Its current license/evaluation behavior is preserved; no key is embedded by FIGCharts.

## FIGSignalCpp binary reports

Open `.figdata`, `.raw`, or `.bin` files written by FIGSignalCpp's `fig::figdata::Writer` (version 1). Files with the `FIGDATA` signature are also detected regardless of extension. CSV support remains available in the file dialog and command-line render mode.

The reader matches the writer's actual 128-byte header and descriptors, contiguous unaligned little-endian columns, integer volume, and per-column validity bitmaps. It bulk-reads numeric columns without CSV tokenization or decimal parsing. Timestamp and OHLC columns are found by role; the study list comes from the variable column names. Missing study values remain gaps, including entirely missing study columns. Required price fields, timestamps, file size, column sizes and block bounds are validated; incomplete and unsupported files are rejected.

Only numeric study columns are plotted. Dictionary text, metadata, bar/background colors, display-mode hints and panel-group hints are not applied by the current viewer. Existing study-selection/overlay controls remain in use. Files are still loaded into memory synchronously; this change accelerates input parsing, rather than introducing lazy loading or asynchronous rendering.

`--self-test` includes binary compatibility and malformed-file regression checks. The existing `--render` and `--ui-test` modes also accept FIGDATA input.
## Settings and menus

**Help → Settings** controls the default chart type (OHLC initially), light/dark theme, UTC/local/named Windows time zone, chart title, and legend visibility. Apply settings updates the current chart and saves preferences in `%LOCALAPPDATA%/FIGCharts/preferences.txt`. Native system menus and file dialogs retain Windows styling.

Dates on the chart, in the legend, and in Go To follow the selected zone. Underlying timestamps stay in UTC. Conversion applies Windows historical daylight-saving rules; nonexistent local times are rejected. For an ambiguous repeated autumn time, Windows chooses its standard-time mapping. Select UTC to target either occurrence explicitly. Legend and crosshair values use two decimal places; stored numeric precision is unchanged.

The legend columns are Study, Value, Display, Color, Line, Thickness and Delete, in that order. Each study row uses its configured color. Display uses a Text checkbox; Color shows a clickable color swatch; Line and Thickness have in-cell dropdowns. Thickness is in points, rendered at a 96-DPI pixel scale. The previous bottom control strip is removed. Keyboard shortcuts on a selected row: Space toggles Text, F2 opens Line, and Delete removes the row. Scrolling or resizing cancels an active dropdown without changing its value.


- **File:** Open CSV, Open binary, Open data, Export PNG, Refresh file, Exit. Refresh rereads the same file and retains the viewport where possible.
- **Edit:** Add studies (opens the filtered study picker), Copy chart image, Add/edit title, Edit selected study. Copy puts the rendered chart on the Windows clipboard; the transient crosshair is excluded.
- **View:** Zoom in/out/all, Go to end, Go to date/time, Enable/Disable legend.
- **Help:** About and Settings.

Financial statistics, signal statistics and executing FIGSignalSvc are deferred future features and are not exposed in this version.
### Add studies from the legend

Click **Add study...** above the legend table (also **Edit → Add studies**). The picker lists only studies not already added to the legend. Selecting a name does not add it; use **Add overlay** or **Add text**. The picker stays open for additional selections and refreshes after each addition. Deleting a legend row returns that study to the picker. If all studies are already added, the picker is empty and the add buttons are disabled. The existing main-window study list and explicit add buttons remain available.
## Financial legend and signal colors

Use **Edit > Add Financial Legend**, select a report study ending in `_SIDE`, adjust sizing inputs, and press **Apply / Calculate**. Move the chart crosshair to inspect cumulative statistics through that exact source bar. Without a crosshair, values refer to the last visible bar. Closing the financial window hides the window; **Remove financial legend** clears the selection and restores standard price coloring.

- Entry occurs at the close of the first nonzero SIDE bar. Exit occurs at the close of the first zero bar. Every nonzero SIDE change closes the old trade and opens a new trade at the same bar close, including 1.1 to 1.2. Positive values are long; negative values are short. The numerical magnitude is a signal category, not a position multiplier.
- Calculation begins at the first loaded bar with no preexisting position. An initial nonzero SIDE opens a trade there. A final nonzero SIDE remains open. Missing/non-finite SIDE values leave the prior position unchanged and are reported in the financial window.
- Defaults from the supplied EFS: initial AUM 1,200,000; multiplier 20; commission 1.60 per unit on every entry and exit; allocation 100%; reinvestment off. Fixed quantity 0 means automatic sizing: `ceil(capital * allocation / multiplier / ((high + low + close) / 3))`. Fixed positive quantity overrides automatic sizing. When reinvestment is enabled, capital includes realized net P/L. No slippage is assumed.
- Gross closed P/L is `(exit - entry) * signed quantity * multiplier`. Open P/L is marked to each bar close. Commission is accumulated separately as a negative number. `Net = NetClosePL + NetOpenPL + Commission` and `NAV = 100 * (AUM + Net) / AUM`.
- NAVMax is the running peak starting at 100. NAVDrawDown is NAV minus its peak with reinvestment off; with reinvestment on it is the percentage decline from the peak. MaxDrawDown is the most negative drawdown. NAV_Trade and its peak/drawdowns update only on bars that close a trade, including reversal bars after both fills.
- Per-side `_bar_Frq` is the text `active-bar-count_signal-entry-count` (the underscore is a separator, not a decimal point). Per-side `_ClosePL` is cumulative gross closed P/L for that exact SIDE value. `_TradePrice` is the fill price on entry/exit bars and zero otherwise.
- Every observed nonzero SIDE has its own color and Show checkbox. Unchecked sides and zero-SIDE bars are white. Hidden sides are also omitted from the individual financial legend rows; overall totals still include every trade. Light theme uses a contrasting neutral plot background while signal coloring is active so white bars remain visible.
- Financial results are computed from all source bars, not the overview buckets. At very wide zoom levels the existing display grouping retains OHLC extrema/summed volume and uses the last SIDE in each bucket for color; zoom in to inspect individual signal transitions. Ordinary study lines likewise use each bucket's final value.

Per-side `_W_L` displays winning closed-trade gross P/L and count, then losing closed-trade gross P/L and count: `1500.00 (8) / -420.00 (3)`. Zero-P/L trades count as wins; open trades are not counted until closed. Commission remains separate, consistent with `_ClosePL`. Values reflect only trades closed through the selected bar.

### Background color rules
After loading CSV or FIGDATA, open **Edit > Background color rules**. Create a rule, select its condition in the tree, choose a study, comparison (`>`, `<`, `=`, `>=`, `<=`, `!=`) and numeric value, then click **Update selected**. Add conditions or nested AND/OR/NOT groups and choose a background color. NOT negates the conjunction of its children.

Rules run from top to bottom; the first enabled match colors that bar's price-chart background. Move rules up/down to set priority. Bars with no match keep the theme background. Empty groups, unavailable studies and missing values do not match. **Test / Preview** paints the chart and reports matching bars without saving; **Close** restores the last applied rules. **Apply** saves rules with application settings and workspaces. Rules are evaluated once per dataset/rule change; when zoomed far out, each aggregated bar uses the last source bar's background color.

### Workspaces
Use **File > Workspace > Save Workspace** to save a `.figworkspace` file, or **Save Workspace As...** for a separate copy. **Load Workspace...** restores the dataset path, applied chart preferences/title, viewport and volume state, study order/styles/selection, background rules, financial calculation settings and SIDE colors/visibility, legend advanced/compact modes, study search/picker state, window sizes/positions/visibility, and table column widths. Data is referenced, not copied into the workspace. Keep the referenced data file available.

FIGCharts opens the last successfully loaded/saved workspace on startup. Closing the application or switching workspaces offers Save / Discard / Cancel when the current state differs from the saved state; a new changed workspace requests a filename. Apply edits in settings/rule/financial dialogs before saving. Saved window positions are constrained to available monitors. The Refresh Data File toolbar button after Open Binary reloads the current data while retaining the chart view and selected study settings.

Use **View > Signal performance report...** after calculating a financial SIDE study to list all signal entries, including hidden SIDE values. Rows start ordered by net P/L (highest first); click a column heading to sort. Each row uses its financial legend SIDE color. Double-click a row, press Enter, or select **Go to signal** to center the chart on its entry. Return % is net P/L divided by entry notional; open entries include entry commission and are marked at the latest close.

Drag beyond either edge to leave up to two thirds of the viewport empty. Price and study layers are clipped to their plot areas. Financial SIDE colors are keyed by their numeric value and retained for sides absent from the current data.

The signal report uses CSV and copy icons. The first CSV export asks for a filename; later exports overwrite that destination. Change it in Settings > Signal report CSV. The destination and independent A-/A+ table font sizes for both legends and the signal report are stored when saving the workspace. Financial summary pins the SIDE (four decimals), total bars, signal frequency, and closed P/L along with account metrics.

### Database prices and studies
In **Edit > Settings**, existing options are on **General**. On **Database**, enter an ODBC SQL Server connection string, use **Test connection**, then **Apply settings**. Example: `Driver={ODBC Driver 18 for SQL Server};Server=localhost;Database=FIG;Trusted_Connection=Yes;Encrypt=Yes;`. Install the matching x64 SQL Server ODBC driver. The connection string is stored in local application preferences; use Windows authentication when possible.

Use **File > Get prices from database** or the database toolbar icon to choose an enabled StudyCol (ID, ticker, interval, collection type). Reads use committed StudyHistory rows. FIGS v1 payloads are validated and every override entry replaces its base entry, including nulls. Numeric and Boolean values become chart study series; null and string values display as gaps. UTC timestamps remain unchanged.

Database charts load **all history**, including when reopening workspaces created with an older recent-bars limit. Full-history financial calculations remain available.

**Refresh data** rereads the full collection and replaces the chart data, including revised existing rows and regenerated history, while retaining the viewport by UTC timestamp and study settings. A chart at the end follows the latest bars. There is no automatic polling. Workspaces reference the collection ID and use the application's configured database connection, without embedding credentials in the workspace.

Database reads bind fixed price fields once and stream complete binary payloads. For large collections, SQL Server should have an index starting with `(StudyColId, RawTime)`; the shipped FIG schema defines this index.

The database reader reuses binary buffers, decoded study schemas and chart column mappings across rows instead of building and merging dictionaries for each bar. FIGS validation still runs, including duplicate names, UTF-8, tags, lengths and decimal flags. Every override is applied after base values, including nulls. `FIGCharts.exe --benchmark-study-decode output.txt` compares the dictionary and reusable paths on 50,000 synthetic bars with 64 studies, verifies matching values, and reports decoding/series-building timings; it does not measure SQL Server or network time.

### Database load performance
Database loading reads both `Studies` and `StudiesOverride`. Every override entry replaces its base value, including null overrides; SQL NULL means there is no override payload. All history remains loaded.

Enable **Settings > Database > Show performance report after database load / refresh** to open the report automatically (off by default). A **Database load performance** report shows bar/study counts, combined base and override payload MiB, connection time, query prepare/execute, row/blob fetching, FIGS decoding, chart series construction, and chart setup/calculation/rendering time. Reopen it from **View > Database load performance**; Ctrl+C copies the report. Loading a workspace records reader timings, but chart setup timing is available when using the database load/refresh command. Timings are measured on the client and include driver/network waits; they are not a SQL Server execution profile.
