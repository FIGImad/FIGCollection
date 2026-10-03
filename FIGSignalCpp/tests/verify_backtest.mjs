// Independent reconciliation of fixed/EFS sizing, zero-slippage, non-forced runs against study CSV.
// node tests/verify_backtest.mjs study.csv batch/reports
import fs from 'node:fs';
import path from 'node:path';
function csv(filename) {
  const lines = fs.readFileSync(filename, 'utf8').trim().split(/\r?\n/);
  function fields(line) {
    const out = []; let value = '', quoted = false;
    for (let i = 0; i < line.length; ++i) {
      if (line[i] === '"') {
        if (quoted && line[i + 1] === '"') { value += '"'; ++i; } else quoted = !quoted;
      } else if (line[i] === ',' && !quoted) { out.push(value); value = ''; }
      else value += line[i];
    }
    out.push(value); return out;
  }
  const keys = fields(lines.shift());
  return lines.map(line => Object.fromEntries(fields(line).map((v, i) => [keys[i], v])));
}
const [studyFile, dir] = process.argv.slice(2);
if (!studyFile || !dir) throw new Error('Provide study CSV and reports directory');
const bars = csv(studyFile), summary = csv(path.join(dir, 'results.csv')).find(r => r.RunId === 'run-0001');
const monthly = csv(path.join(dir, 'run-0001-monthly.csv'));
const trades = csv(path.join(dir, 'run-0001-trades.csv'));
const settings = Object.fromEntries(fs.readFileSync(path.join(dir, 'batch-settings.txt'), 'utf8').trim().split(/\r?\n/).map(s => { const n = s.indexOf('='); return [s.slice(0,n),s.slice(n+1)]; }));
const capital = Number(settings.InitialCapital), contracts = Number(settings.Contracts), multiplier = Number(settings.Multiplier), fee = Number(settings.CommissionPerContractPerSide);
const allocation = Number(settings.QuantityPct ?? 1);
if (Number(settings.SlippagePointsPerSide) !== 0 || Number(settings.ForceClose) !== 0) throw new Error('This reconciliation expects zero slippage and no forced close');
const start = Date.parse(settings.StartUTC + 'T00:00:00Z') / 1000;
let quantity = 0, entry = 0, entryTime = 0, cash = capital, fees = 0, realized = 0, equity = capital, peak = capital, dd = 0;
const expectedTrades = [], months = new Map();
function near(a, b, label) { if (Math.abs(Number(a) - Number(b)) > 1e-6) throw new Error(`${label}: ${a} != ${b}`); }
for (const b of bars) {
  const close = Number(b.Close), time = Number(b.RawTime), side = Number(b.STrend_LBaseADX_Side);
  if (time < start) continue;
  const month = new Date(time * 1000).toISOString().slice(0, 7);
  if (!months.has(month)) months.set(month, {start: equity, end: equity, fees: 0});
  const m = months.get(month);
  if (quantity === 0 && side === 1) {
    quantity = contracts || Math.ceil(capital * allocation / multiplier / ((Number(b.High) + Number(b.Low) + close) / 3));
    entry = close; entryTime = time;
    cash -= quantity * fee; fees += quantity * fee; m.fees += quantity * fee;
  } else if (quantity > 0 && side === 0) {
    const profit = (close - entry) * quantity * multiplier;
    cash += profit - quantity * fee; realized += profit;
    fees += quantity * fee; m.fees += quantity * fee;
    expectedTrades.push({entryTime, time, quantity, profit: profit - 2 * quantity * fee}); quantity = 0;
  }
  equity = cash + quantity * multiplier * (close - entry);
  m.end = equity;
  peak = Math.max(peak, equity); dd = Math.min(dd, 100 * (equity / peak - 1));
}
near(summary.FinalNAV, equity * 100 / capital, 'NAV');
near(summary.NetProfit, equity - capital, 'net');
near(summary.Commissions, fees, 'fees');
near(summary.RealizedGrossProfit, realized, 'realized');
near(summary.MaxDrawdownPct, dd, 'drawdown');
near(summary.OpenContracts, quantity, 'position');
near(trades.length, expectedTrades.length, 'trades');
trades.forEach((t, i) => {
  near(t.EntryRawTime, expectedTrades[i].entryTime, 'entry time');
  near(t.ExitRawTime, expectedTrades[i].time, 'exit time');
  near(t.Contracts, expectedTrades[i].quantity, 'entry quantity');
  near(t.NetProfit, expectedTrades[i].profit, 'trade net');
});
for (const m of monthly) {
  const expected = months.get(m.MonthUTC);
  if (!expected) throw new Error(`Unexpected month ${m.MonthUTC}`);
  near(m.StartEquity, expected.start, 'monthly start');
  near(m.EndEquity, expected.end, 'monthly end');
  near(m.MonthlyNetProfit, expected.end - expected.start, 'monthly net');
  near(m.Commissions, expected.fees, 'monthly fees');
}
console.log(`Independent backtest passed: ${bars.length} bars, ${trades.length} closed trades, ${monthly.length} monthly rows.`);
