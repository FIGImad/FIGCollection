// Offline integration regression: actual CLI candidate validation and worker creation.
// A synthetic price cache avoids database credentials/connections entirely.
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const build = path.join(root, 'x64', 'Release');
const dir = fs.mkdtempSync(path.join(build, 'ladx-batch-test-'));
const settings = path.join(dir, 'settings.json');
const text = '{"ODBCName":"OfflineRegressionUnused"}';
fs.writeFileSync(settings, text);
const cache = path.join(dir, 'prices');
const parameters = path.join(dir, 'parameters');
fs.mkdirSync(cache); fs.mkdirSync(parameters);
fs.writeFileSync(path.join(parameters, 'a.json'), '{}');
fs.writeFileSync(path.join(parameters, 'b.json'), '{"indicators":{"adxLength":41}}');
function hash(data) {
    let h = 14695981039346656037n;
    for (const b of data) h = BigInt.asUintN(64, (h ^ BigInt(b)) * 1099511628211n);
    return h;
}
const count = 3000, data = Buffer.alloc(count * 56);
for (let i = 0; i < count; ++i) {
    const offset = i * 56, price = 100 + (i % 71) * .25;
    data.writeInt32LE(i, offset); data.writeInt32LE(3, offset + 4);
    data.writeBigInt64LE(1704067200n + BigInt(i * 60), offset + 8);
    for (const [n, value] of [price, price + 1, price - 1, price + .125].entries()) data.writeDoubleLE(value, offset + 16 + n * 8);
    data.writeBigInt64LE(100n, offset + 48);
}
const header = Buffer.alloc(40);
[0x4649475043410001n, BigInt(count), BigInt(count), hash(data), 8n].forEach((v, i) => header.writeBigUInt64LE(v, i * 8));
const identity = hash(Buffer.from(settings + '\n' + text));
fs.writeFileSync(path.join(cache, `${identity}-d3-n0-raw-v1.bin`), Buffer.concat([header, data]));
for (const interval of [5, 10]) {
    const output = path.join(dir, `output-${interval}`);
    const result = spawnSync(path.join(build, 'FIGSignalCpp.exe'), [
        '--backtest-batch', '--collection-dll', path.join(build, 'FIGStudyCollection.DotNetLADX.dll'),
        '--collection', 'LADX', '--signal', 'LADX_SIDE', '--interval-minutes', String(interval),
        '--parameters-dir', parameters, '--output-dir', output, '--settings', settings,
        '--price-cache-dir', cache, '--price-cache-mode', 'use', '--jobs', '2',
    ], { encoding: 'utf8', env: { ...process.env, FIG_LADX_CACHE: path.join(dir, 'managed-cache') } });
    if (result.status !== 0) throw new Error(result.stdout + result.stderr);
    const rows = fs.readFileSync(path.join(output, 'results.csv'), 'utf8').trim().split(/\r?\n/);
    if (rows.length !== 3) throw new Error('Expected both candidates to finish');
    if (!fs.readFileSync(path.join(output, 'batch-settings.txt'), 'utf8').includes(`IntervalMinutes=${interval}`)) throw new Error('Wrong batch interval');
    console.log(`PASS: ${interval}-minute .NET LADX batch, two concurrent candidates`);
}
console.log(`Artifacts: ${dir}`);
