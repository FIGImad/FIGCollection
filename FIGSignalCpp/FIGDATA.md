# FIGDATA v1 chart files

Backtests automatically write `reports/run-NNNN-chart.figdata` instead of the chart CSV. The writer uses the exact study values consumed by the ledger, after processing all pre-start warmup bars. Rows include every available aggregated bar in the selected date range, including bars without signals. Existing trade/monthly/results and shared price CSV reports are unchanged.

The packed C++ declarations are in `include/fig/studies/figdata.hpp`, namespace `fig::figdata`. Both `FileHeader` and `ColumnDescriptor` are exactly 128 bytes and match the supplied v1 schema. All integers and IEEE-754 Float64 values are **little-endian**. Reserved bytes and flags are zero. Magic is the eight bytes `46 49 47 44 41 54 41 00` (`FIGDATA` followed by NUL). Version is 1.

## Reading

1. Validate magic, version, header/descriptor sizes, `fileBytes` against the actual size, and all offsets/lengths before accessing data.
2. Read `columnCount` descriptors at `columnsOffset`. Names and optional titles reference UTF-8 bytes relative to `stringsOffset`, without NUL terminators. This writer leaves titles empty, meaning use the name.
3. Each data block contains `rowCount` contiguous values, with no row headers or stride padding: 8 bytes for Int64/Float64 and 4 bytes for UInt32/DictionaryText. Offsets are absolute. Blocks are not guaranteed to be 8-byte aligned; use unaligned-safe reads rather than casting file addresses.
4. `validityOffset == 0` and `validityBytes == 0` mean all rows are valid. Otherwise there are `ceil(rowCount / 8)` bytes. Row `i` is valid when `(bitmap[i / 8] & (1 << (i % 8))) != 0`. Unused high bits of the final byte are zero. Missing numeric values have zero payload but a cleared validity bit; zero alone never means missing.
5. DictionaryText data contains zero-based uint32 codes. Its dictionary block is a little-endian `uint32 entryCount`, followed by `entryCount` entries, each `uint32 byteLength` then exactly that many UTF-8 bytes. Entries have no terminators, padding, or alignment. Codes refer to entry order. An empty string is a valid dictionary entry, distinct from a missing value. `dictionaryBytes` covers the count, lengths, and strings. Numeric columns have zero dictionary offsets/lengths.

## Columns and presentation

- `Id`, `DataSetId`: Int64 metadata. Aggregated IDs can be -1.
- `RawTime`: Int64 with Timestamp role; Unix seconds UTC. Human-readable time is derived by the reader. `firstTimestamp` and `lastTimestamp` describe the first/last exported rows.
- `Open`, `High`, `Low`, `Close`: Float64 with the corresponding roles. `Volume`: Int64 with Volume role, preserving the source integer exactly.
- `SignalColor`: UInt32 metadata; `PriceBarColor`: UInt32 BarColor; `BackgroundColor`: UInt32 Background. Values are `0xAARRGGBB`, not RGB byte strings. LADX2 uses opaque colors: side 1.1 `0xFF228B22`, side 1.2 `0xFFBEBE00`, otherwise `0xFFFFFFFF`. Ready market trends use `0xFF00280A` (positive), `0xFF28000A` (negative), and `0xFF000000` (neutral/not ready). Other collections currently have invalid color values, allowing the reader’s default styling.
- Every numeric study: Float64 with Study role; absent optionals have invalid bits. Numeric flags retain their original numeric values.
- Every text study: DictionaryText with Study role.

The interval is stored once as `intervalSeconds` (e.g. 300), not repeated per row. LADX2 `LADX2_PriceMA`, `LADX2_GuideMA`, `LADX2_GuideUB`, and `LADX2_GuideLB` use PriceOverlay display with the EFS light-green/lavender colors. Other numeric studies default to StudyPanel and automatic color; `panelGroup == 0` means their own panel. Text and metadata have None display. Readers may override presentation.

The writer streams small column buffers into reserved data ranges, then appends validity bitmaps, dictionaries, and names. Descriptors and the valid header are finalized last. Failed/incomplete exports must not be loaded. Output files are complete only when the run succeeds.
