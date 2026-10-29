// Validate every OHLCV row and every closed signal trade; normalize color serialization.
const fs = require('node:fs');
const path = require('node:path');
const readline = require('node:readline');
const {once} = require('node:events');
async function* lines(file) {
  const input = fs.createReadStream(file);
  for await (const line of readline.createInterface({input, crlfDelay: Infinity})) yield line;
}
async function main() {
  const [batch, dir] = process.argv.slice(2);
  const summary = JSON.parse(fs.readFileSync(path.join(dir, 'export-summary.json')));
  for (const run of summary.runs) {
    const trades = fs.readFileSync(path.join(batch, 'reports', run.id + '-trades.csv'), 'utf8').trim().split(/\r?\n/).slice(1).map(x => x.split(','));
    const prices = lines(path.join(batch, 'reports/prices.csv'))[Symbol.asyncIterator]();
    await prices.next();
    let price = await prices.next();
    let header, count = 0, position = false, entry, tradeIndex = 0;
    const colors = {};
    const out = fs.createWriteStream(run.output + '.verified');
    for await (const line of lines(run.output)) {
      let corrected = line;
      if (!header) header = line.split(',');
      else {
        const row = line.split(',');
        const time = Number(row[2]);
        while (!price.done && Number(price.value.split(',')[2]) < time) price = await prices.next();
        if (price.done || row.slice(0, 8).join(',') !== price.value) throw Error('OHLCV mismatch');
        const side = Number(row[header.indexOf('LADX2_SIDE')]);
        const color = summary.colors[String(side)] || summary.colors.other;
        row[header.indexOf('SignalColor')] = color;
        row[header.indexOf('PriceBarColor')] = color;
        colors[color] = (colors[color] || 0) + 1;
        if (position && side === 0) {
          const trade = trades[tradeIndex++];
          if (!trade || Number(trade[0]) !== entry || Number(trade[1]) !== time) throw Error('Trade signal mismatch');
          position = false;
        }
        if (!position && side !== 0) { entry = time; position = true; }
        corrected = row.join(','); count++;
      }
      if (!out.write(corrected + '\n')) await once(out, 'drain');
    }
    out.end(); await once(out, 'finish');
    await prices.return();
    if (count !== run.bars || tradeIndex !== trades.length) throw Error('Row/trade count mismatch');
    fs.renameSync(run.output + '.verified', run.output);
    console.log(JSON.stringify({run: run.id, bars: count, matchedClosedTrades: tradeIndex, colors}));
  }
}
main().catch(e => {console.error(e); process.exitCode = 1;});
