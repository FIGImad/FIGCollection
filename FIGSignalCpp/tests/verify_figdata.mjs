// node tests/verify_figdata.mjs chart.figdata reference-chart.csv
// Independent binary reader: validates the v1 contract and compares every value.
import fs from 'node:fs';
import assert from 'node:assert/strict';
const [binaryPath, csvPath] = process.argv.slice(2);
const b = fs.readFileSync(binaryPath);
const u32 = offset => b.readUInt32LE(offset);
const u64 = offset => {
  const n = b.readBigUInt64LE(offset);
  assert(n <= BigInt(Number.MAX_SAFE_INTEGER));
  return Number(n);
};
const i64 = offset => Number(b.readBigInt64LE(offset));
const ranges = [];
function range(offset, bytes, label) {
  assert(offset >= 0 && bytes >= 0 && offset + bytes <= b.length, label);
  if (bytes) ranges.push({offset, end: offset + bytes, label});
}
function zero(offset, bytes) { assert(b.subarray(offset, offset + bytes).every(v => v === 0)); }
function parse(line) {
  const values = []; let value = '', quoted = false;
  for (let i = 0; i < line.length; i++) {
    if (line[i] === '"') {
      if (quoted && line[i + 1] === '"') { value += '"'; i++; }
      else quoted = !quoted;
    } else if (line[i] === ',' && !quoted) { values.push(value); value = ''; }
    else value += line[i];
  }
  assert(!quoted); values.push(value); return values;
}
assert.equal(b.subarray(0, 8).toString(), 'FIGDATA\0');
assert.equal(u32(8), 1); assert.equal(u32(12), 128);
const count = u64(16), columns = u32(24), descriptors = u64(32), strings = u64(40);
assert.equal(u32(28), 128); assert.equal(u64(56), b.length);
assert.equal(u32(84), 0); zero(88, 40);
range(0, 128, 'header'); range(descriptors, columns * 128, 'descriptors');
range(strings, u64(48), 'names');
const lines = fs.readFileSync(csvPath, 'utf8').trim().split(/\r?\n/);
const names = parse(lines.shift()), reference = lines.map(parse);
assert.equal(count, reference.length);
assert.equal(columns, names.length - 2); // TimeUTC and IntervalMinutes now live in the header/timestamp.
assert.equal(i64(64), Number(reference[0][names.indexOf('RawTime')]));
assert.equal(i64(72), Number(reference.at(-1)[names.indexOf('RawTime')]));
assert.equal(u32(80), Number(reference[0][names.indexOf('IntervalMinutes')]) * 60);
const seen = new Set();
for (let c = 0; c < columns; c++) {
  const d = descriptors + c * 128;
  const nameOffset = u64(d), nameBytes = u32(d + 8);
  assert(nameOffset + nameBytes <= u64(48));
  const name = b.subarray(strings + nameOffset, strings + nameOffset + nameBytes).toString('utf8');
  assert(!seen.has(name)); seen.add(name);
  const source = names.indexOf(name); assert(source >= 0, name);
  const type = u32(d + 12), role = u32(d + 28), display = u32(d + 80);
  assert(type >= 1 && type <= 4); assert(role >= 1 && role <= 10); assert(display <= 2);
  assert.equal(u32(d + 92), 0); zero(d + 96, 32);
  const titleOffset = u64(d + 16), titleBytes = u32(d + 24);
  assert(titleOffset + titleBytes <= u64(48));
  const data = u64(d + 32), bytes = u64(d + 40), validity = u64(d + 48), validityBytes = u64(d + 56);
  const width = type <= 2 ? 8 : 4;
  assert.equal(bytes, width * count); range(data, bytes, name);
  if (validity) {
    assert.equal(validityBytes, Math.ceil(count / 8)); range(validity, validityBytes, name + ' validity');
    if (count % 8) assert.equal(b[validity + validityBytes - 1] >> (count % 8), 0);
  } else assert.equal(validityBytes, 0);
  const dictionary = [];
  if (type === 4) {
    let cursor = u64(d + 64); const end = cursor + u64(d + 72);
    range(cursor, end - cursor, name + ' dictionary');
    const entries = u32(cursor); cursor += 4;
    for (let i = 0; i < entries; i++) {
      assert(cursor + 4 <= end); const length = u32(cursor); cursor += 4;
      assert(cursor + length <= end); dictionary.push(b.subarray(cursor, cursor + length).toString('utf8')); cursor += length;
    }
    assert.equal(cursor, end);
  } else { assert.equal(u64(d + 64), 0); assert.equal(u64(d + 72), 0); }
  if (name === 'RawTime') assert.equal(role, 1);
  if (name === 'PriceBarColor') assert.equal(role, 9);
  if (name === 'BackgroundColor') assert.equal(role, 10);
  if (['LADX2_PriceMA','LADX2_GuideMA','LADX2_GuideUB','LADX2_GuideLB'].includes(name)) assert.equal(display, 1);
  for (let r = 0; r < count; r++) {
    const expected = reference[r][source];
    const valid = !validity || Boolean(b[validity + Math.floor(r / 8)] & (1 << (r % 8)));
    if (type !== 4) assert.equal(valid, expected !== '', `${name} validity at ${r}`);
    if (!valid) { assert.equal(expected, '', `${name} missing at ${r}`); continue; }
    const offset = data + r * width;
    let actual = type === 1 ? i64(offset) : type === 2 ? b.readDoubleLE(offset) : u32(offset);
    if (type === 4) { assert(actual < dictionary.length); actual = dictionary[actual]; }
    const converted = type === 4 ? expected : expected.startsWith('#') ? Number.parseInt('FF' + expected.slice(1), 16) : Number(expected);
    assert.equal(actual, converted, `${name} row ${r}`);
  }
}
ranges.sort((a, z) => a.offset - z.offset);
for (let i = 1; i < ranges.length; i++) assert(ranges[i].offset >= ranges[i - 1].end, 'Overlapping blocks');
console.log(`FIGDATA validated: ${count} rows, ${columns} columns, ${b.length} bytes; all values match CSV.`);
