# FIGSignalCpp

Native Windows/C++ rewrite of `FIGSignalSvc`. It is designed as one executable: study collections are registered at compile time and there is no assembly, manifest, or DLL loading.

## Current parity gate

Build x64 and run `FIGSignalCpp.exe --self-test`. A successful result verifies the first native primitives (five-minute OHLCV aggregation, MA, WMA, and core model behavior).

This directory is an active port. It must not replace `FIGSignalSvc` in production until the parity checklist in `PORTING.md` is complete.

## Live study rule

Every native study is a separate class under `include/fig/studies` with its implementation under `src/studies`. `process(...)` is the primary API and consumes one timestamped bar (or one timestamped upstream study value). The class retains its rolling state and a bounded rollback history so a live current-bar update replaces that bar rather than appending or recalculating all history. Collection startup uses `StudyCollection::replay`, which feeds historical bars through the same live path.

## Formatting

The project uses the checked-in `.clang-format` file: C++20, four-space indentation, no tabs, Allman braces, and a 120-column limit. Format new or modified C++ files with `clang-format -i --style=file` before building.
