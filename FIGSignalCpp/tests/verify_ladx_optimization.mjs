// Validate the prepared 200-case batch and every candidate against the actual LADX DLL.
// Uses synthetic three-minute bars; does not access SQL or claim historical performance.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const batch = path.resolve(process.argv[2] ?? path.join(root, 'optimizer-candidates/ladx-3m-200'));
const runner = path.resolve(process.argv[3] ?? path.join(root, 'x64/Release/optimizer/FIGOptimizationRunner.exe'));
const manifest = JSON.parse(fs.readFileSync(path.join(batch, 'batch.json'), 'utf8'));
const config = manifest.Configuration;
assert.equal(config.Collection, 'LADX');
assert.equal(config.Signal, 'LADX_LongSide');
assert.equal(config.IntervalMinutes, 3);
assert.equal(config.Samples, 199);
assert.equal(manifest.Candidates.length, 200);
const baseline = JSON.parse(fs.readFileSync(config.BaseParameters, 'utf8'));
const sha = file => createHash('sha256').update(fs.readFileSync(file)).digest('hex').toUpperCase();
assert.equal(manifest.CollectionDllSHA256, sha(config.CollectionDll));
assert.equal(manifest.ExecutableSHA256, sha(config.Executable));
const output = fs.mkdtempSync(path.join(root, 'optimizer-output/ladx-candidate-check-'));
function run(executable, args) {
  const r = spawnSync(executable, args, {encoding:'utf8', windowsHide:true, cwd:root});
  if (r.error) throw r.error;
  assert.equal(r.status, 0, `${executable}: ${r.stdout}\n${r.stderr}`);
}
// Preparing the saved configuration must reproduce every candidate and its hash.
const repeated = path.join(output, 'repeat');
run(runner, ['prepare', '--config', path.join(batch, 'search-settings.json'), '--output-dir', repeated]);
assert.deepEqual(JSON.parse(fs.readFileSync(path.join(repeated, 'batch.json'), 'utf8')).Candidates, manifest.Candidates);
let seed = 23, ticks = 4000;
const random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed; };
const bars = Array.from({length:600}, (_, n) => {
  const open = ticks / 4;
  ticks += (n % 240 < 120 ? 3 : -3) + random() % 17 - 8;
  if (n % 179 === 0) ticks -= 120;
  if (n % 227 === 0) ticks += 160;
  const close = ticks / 4;
  return [1609459200 + n * 180, open, Math.max(open, close) + (1 + random() % 16) / 4,
    Math.min(open, close) - (1 + random() % 16) / 4, close, 100 + random() % 900];
});
const priceFile = path.join(output, 'prices-3m.csv');
fs.writeFileSync(priceFile, 'RawTime,Open,High,Low,Close,Volume\n' + bars.map(row => row.join(',')).join('\n') + '\n');
const identities = new Set();
let signalRows = 0, activeRows = 0;
for (const [index, candidate] of manifest.Candidates.entries()) {
  const file = path.join(batch, 'parameters', candidate.FileName);
  assert.equal(sha(file), candidate.SHA256);
  const parameters = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (index === 0) assert.deepEqual(parameters, baseline, 'case 1 must equal the base values');
  assert.deepEqual(Object.keys(parameters).sort(), Object.keys(baseline).sort());
  for (const [key, value] of Object.entries(parameters)) {
    if (!Object.hasOwn(config.Ranges, key)) assert.equal(value, baseline[key], `${key} must stay fixed`);
    else assert(config.Ranges[key].includes(value), `${key} is outside the configured choices`);
  }
  const identity = JSON.stringify(Object.fromEntries(Object.entries(parameters).sort(([a],[b]) => a.localeCompare(b))));
  assert(!identities.has(identity), 'duplicate candidate');
  identities.add(identity);
  const values = path.join(output, candidate.FileName.replace('.json', '-values.csv'));
  run(path.join(root, 'x64/Release/FIGCollectionProbe.exe'), [config.CollectionDll, config.Collection, file, priceFile, values]);
  // LADX field names and reason strings contain no commas; quotes are CSV wrappers only.
  const rows = fs.readFileSync(values, 'utf8').trim().split(/\r?\n/).map(line => line.split(',').map(x => x.replace(/^"|"$/g, '')));
  const header = rows.shift();
  const signal = header.indexOf(config.Signal), side = header.indexOf('LADX_SIDE');
  assert(signal >= 0 && side >= 0);
  assert.equal(rows.length, bars.length);
  for (const [n, row] of rows.entries()) {
    assert.equal(Number(row[0]), bars[n][0]);
    assert(['0', '1'].includes(row[signal]), 'backtest signal must be numeric 0 or 1 on every bar');
    assert.equal(Number(row[signal]), Number(row[side]) > 0 ? 1 : 0);
    signalRows++;
    activeRows += Number(row[signal]);
  }
}
assert(activeRows > 0, 'synthetic inputs must exercise active long signals');
console.log(`LADX optimization passed: 200 unique reproducible cases; ${signalRows} native signal rows checked (${activeRows} active).`);
console.log(`Validation artifacts: ${output}`);
