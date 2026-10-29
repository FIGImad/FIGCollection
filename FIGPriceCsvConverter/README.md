# FIG Price CSV Converter

Windows desktop tool for converting eSignal price exports to FIGSignalBacktest CSV input.

1. Open `FIGPriceCsvConverter.exe` and browse to the eSignal export.
2. Choose a target CSV. The suggested name ends in `-fig.csv`.
3. Select the time zone used by eSignal when exporting. **Local computer** uses this computer's configured zone; it does not detect the export's zone. Named zones apply historical daylight-saving rules.
4. Leave ambiguous times set to **Stop and report** unless you know whether the export represents the earlier or later occurrence during a fall-back clock change.
5. Click **Convert CSV**. The app shows the UTC range, row count, and first eight output rows. **Show output file** locates the result in Explorer.

Input requires `Date,Time,Open,High,Low,Close,Vol` (`Volume` is also accepted). Dates use `yyyy-MM-dd` or US `M/d/yyyy` (for example, `10/28/2026`); slash dates always mean month/day/year regardless of computer regional settings. Times may use `10:37:00 PM` or `22:37:00`. Headers are case-insensitive; quoted fields and additional columns such as Bar#, Bar Index, and Tick Range are supported. Prices use decimal points and volume is an integer.

Output is UTF-8 `RawTime,Open,High,Low,Close,Volume`, with UTC Unix seconds and strictly increasing timestamps. Rows are sorted oldest first regardless of input order. Prices are preserved without aggregation. No DataSetId is needed. Set `PricesCsv` in the optimizer configuration to the converted file, or pass `--prices-csv` to FIGSignalBacktest.

Nonexistent spring-forward times, duplicate UTC timestamps, invalid prices, and malformed rows stop conversion with an explanation. If an export contains both occurrences of a repeated local time, Date/Time alone cannot distinguish them reliably; export in UTC from eSignal instead. The converter does not silently drop or combine rows.

Existing targets require confirmation. Output is written to a temporary file in the target folder and moved into place only after success. Errors and cancellation preserve the existing target; the source is never overwritten. Rows are held in memory for sorting, so memory usage grows with export size. Sorting must finish before a pending cancellation takes effect.

Build with `dotnet build FIGPriceCsvConverter/FIGPriceCsvConverter.csproj -c Release`. Publish with `dotnet publish FIGPriceCsvConverter/FIGPriceCsvConverter.csproj -c Release -o FIGPriceCsvConverter/publish`. Copy the entire publish folder; the destination needs the .NET 10 Windows Desktop runtime. No database or plugin is required.

Regression checks: `dotnet run --project tests/FIGPriceCsvConverter.Regression -- PATH_TO_TEST_OUTPUT`.
