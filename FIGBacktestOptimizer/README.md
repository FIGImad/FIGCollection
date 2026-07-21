# FIGBacktestOptimizer

Command-line optimizer for the ADXBase 5-minute NQ strategy logic.

The first version is intentionally self-contained instead of using `FIG.Studies.Common`, so overnight optimization is deterministic and does not inherit live-bar replay behavior.

## CSV Input

The input file must have a header row and these columns:

- `DateTime` or `Timestamp`, or separate `Date` and `Time`
- `Open`
- `High`
- `Low`
- `Close`
- optional `Volume`

Example:

```csv
Date,Time,Open,High,Low,Close,Volume
2024-01-02,09:30,17000.25,17025.00,16990.50,17010.75,1234
```

## Usage

Create a sample config:

```powershell
dotnet run --project C:\Users\Imad\source\FIGCollection\FIGBacktestOptimizer -- write-sample-config C:\Temp\adxbase-optimizer.json
```

Edit `DataPath`, `OutputDirectory`, and parameter ranges, then run:

```powershell
dotnet run --project C:\Users\Imad\source\FIGCollection\FIGBacktestOptimizer -- optimize C:\Temp\adxbase-optimizer.json
```

## Outputs

The output directory contains:

- `optimizer-results.csv`: every evaluated parameter set, sorted by composite score
- `top-parameters.json`: top parameter sets with train/validation/test metrics
- `best-trades.csv`: trade list from running the best parameter set over all selected data
- `summary.txt`: compact best-result summary

## Scoring

The optimizer ranks by validation score plus smaller train and walk-forward weights. The score rewards net profit percent and profit factor, and penalizes max drawdown percent, too many trades, and too few trades.

Use the best result as a candidate, not as proof. Confirm it by checking:

- validation and test results are both good
- walk-forward score is not much worse than validation
- neighboring parameter values behave similarly
- trade count is high enough to be meaningful
- results survive realistic slippage
