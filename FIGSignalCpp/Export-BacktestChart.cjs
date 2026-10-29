// Replay the saved backtest snapshot, including warmup, through its original DLL.
// Usage: node Export-BacktestChart.cjs BATCH_DIRECTORY OUTPUT_DIRECTORY [PROBE_EXE]
const fs = require('node:fs');
const path = require('node:path');
const readline = require('node:readline');
const {spawn} = require('node:child_process');
const {once} = require('node:events');
const crypto = require('node:crypto');
async function* rows(file) {
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({input, crlfDelay: Infinity});
  try { for await (const line of lines) yield line; }
  finally { lines.close(); input.destroy(); }
}
async function write(out, line) { if (!out.write(line + '\n')) await once(out, 'drain'); }
async function finish(out) { out.end(); await once(out, 'finish'); }
function cells(line) {
  return (line.match(/(?:"(?:[^"]|"")*"|[^,]*)(?:,|$)/g) || [])
    .slice(0, -1).map(s => s.replace(/,$/, '').replace(/^"|"$/g, '').replace(/""/g, '"'));
}
async function main() {
  const [batchDir, outputDir, probeArg] = process.argv.slice(2);
  if (!batchDir || !outputDir) throw Error('Specify batch and output directories');
  const probe = path.resolve(probeArg || path.join(__dirname, 'x64/Release/FIGCollectionProbe.exe'));
  const batch = JSON.parse(fs.readFileSync(path.join(batchDir, 'batch.json')));
  const config = batch.Configuration;
  if (config.Collection !== 'LADX2') throw Error('Color mappings currently support LADX2 only');
  const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase();
  if (hash(config.CollectionDll) !== batch.CollectionDllSHA256) throw Error('Backtest DLL has changed');
  fs.mkdirSync(outputDir, {recursive: true});
  const prices = path.join(batchDir, batch.ReportDirectory, 'prices.csv');
  const replay = path.join(outputDir, 'replay-prices.csv');
  let out = fs.createWriteStream(replay);
  await write(out, 'RawTime,Open,High,Low,Close,Volume');
  let first = true, total = 0;
  for await (const line of rows(prices)) {
    if (first) {
      if (line !== 'Id,DataSetId,RawTime,Open,High,Low,Close,Volume') throw Error('Unexpected prices header');
      first = false; continue;
    }
    await write(out, line.split(',').slice(2).join(',')); total++;
  }
  await finish(out);
  const start = config.StartDate ? Date.parse(config.StartDate + 'T00:00:00Z') / 1000 : -Infinity;
  const end = config.EndDateExclusive ? Date.parse(config.EndDateExclusive + 'T00:00:00Z') / 1000 : Infinity;
  const summary = {batch: path.resolve(batchDir), intervalMinutes: config.IntervalMinutes,
    warmup: 'All saved bars are replayed before filtering to the backtest date range.',
    colors: {'1.1': '#228B22', '1.2': '#BEBE00', other: '#FFFFFF'}, runs: []};
  for (const candidate of batch.Candidates) {
    const id = path.basename(candidate.FileName, '.json');
    const parameters = path.join(batchDir, 'parameters', candidate.FileName);
    if (hash(parameters) !== candidate.SHA256) throw Error('Candidate parameters have changed');
    const studies = path.join(outputDir, id + '-studies.tmp.csv');
    const child = spawn(probe, [config.CollectionDll, config.Collection, parameters, replay, studies], {stdio: 'inherit', windowsHide: true});
    const [code] = await once(child, 'exit');
    if (code !== 0) throw Error('Collection replay failed: ' + code);
    const iterator = rows(studies)[Symbol.asyncIterator]();
    const header = cells((await iterator.next()).value);
    const sideIndex = header.indexOf('LADX2_SIDE');
    const trendIndex = header.indexOf('MarketTrend');
    const readyIndex = header.indexOf('MarketTrend_READY');
    if (sideIndex < 0 || trendIndex < 0 || readyIndex < 0) throw Error('Missing chart fields');
    const output = path.join(outputDir, id + '-chart.csv');
    out = fs.createWriteStream(output);
    let count = 0, seen = 0, firstTime, lastTime;
    first = true;
    for await (const line of rows(prices)) {
      if (first) {
        await write(out, line + ',TimeUTC,IntervalMinutes,SignalColor,PriceBarColor,BackgroundColor,' + header.slice(1).join(','));
        first = false; continue;
      }
      const next = await iterator.next();
      if (next.done) throw Error('Study output ended early');
      const values = cells(next.value);
      const t = Number(line.split(',')[2]);
      if (Number(values[0]) !== t) throw Error('Price/study timestamp mismatch');
      seen++;
      if (t < start || t >= end) continue;
      const color = summary.colors[String(Number(values[sideIndex]))] || summary.colors.other;
      const trend = Number(values[readyIndex]) === 1 ? Number(values[trendIndex]) : 0;
      const background = trend > 0 ? '#00280A' : trend < 0 ? '#28000A' : '#000000';
      const time = new Date(t * 1000).toISOString();
      await write(out, line + ',' + [time, config.IntervalMinutes, color, color, background].join(',') + ',' + next.value.slice(next.value.indexOf(',') + 1));
      firstTime ||= time; lastTime = time; count++;
    }
    if (!(await iterator.next()).done || seen !== total || count === 0) throw Error('Row count mismatch or empty interval');
    await finish(out);
    fs.unlinkSync(studies);
    summary.runs.push({id, output: path.resolve(output), bars: count, columns: 8 + 5 + header.length - 1, firstTime, lastTime, replayedBars: seen});
    console.log(JSON.stringify(summary.runs.at(-1)));
  }
  fs.unlinkSync(replay);
  fs.writeFileSync(path.join(outputDir, 'export-summary.json'), JSON.stringify(summary, null, 2));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
