# L2 speed regression

Build with `dotnet build -c Release -m:1`, then run the executable with four arguments:

1. Original L2Test DLL (save a copy before rebuilding).
2. Optimized L2Test DLL.
3. Native-compatible raw price-cache file.
4. L2 parameter JSON.

Place matching FIG.Studies.Common.dll beside each plugin. The harness compares every
numeric/text output across up to 36,000 five-minute bars, checks parameter clone
independence, and exercises decimal outputs and repeated rollback/replay. It reports
processing time and cumulative allocations with plugin profiling disabled. The two
versions alternate execution order; times are indicative, not a dedicated benchmark.

The harness also checks StudyExtended across three periods and three price sources,
including checkpoint restore and changing its public label. Each plugin directory
must contain its own version of FIG.Studies.Common.dll to compare shared-library
changes correctly. An optional fifth argument sets bar count (at least 1,000).

For a timing-only comparison, use `--benchmark` followed by the same four paths.
It warms both plugins, runs 12,000 bars per pass with profiling disabled, and
reverses execution order for the second pass. No field comparison is timed.

Set FIG_TEST_COMPARE_REUSE=1 when both plugin directories contain the new build
to compare the fallback Clone path against the reuse path in the same engine.
The harness disables reuse while constructing the reference studies, then enables
it for the second collection. This isolates parameter reuse; both sides already
have the cached circular-buffer view.
