// Compare LADX2 against the unmodified attached EFS study definitions.
// Built-in SMA/ATR series are mocked; this does not claim eSignal platform ATR-seed parity.
import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const configuration = process.argv[2] ?? 'Release';
assert(['Debug', 'Release'].includes(configuration), 'Use Debug or Release');
const bin = path.join(root, 'x64', configuration);
const dll = path.join(bin, 'FIGStudyCollection.LADX2.dll');
const probe = path.join(bin, 'FIGCollectionProbe.exe');
const host = path.join(bin, 'FIGSignalCpp.exe');
for (const file of [dll, probe, host]) assert(fs.existsSync(file), `Build the solution first: ${file}`);
const outputRoot = fs.mkdtempSync(path.join(bin, 'ladx2-parity-'));
const isolatedProbe = path.join(outputRoot, path.basename(probe));
const isolatedDll = path.join(outputRoot, path.basename(dll));
fs.copyFileSync(probe, isolatedProbe);
fs.copyFileSync(dll, isolatedDll);

function run(exe, args, success = true) {
  const result = spawnSync(exe, args, {encoding: 'utf8', windowsHide: true, cwd: outputRoot});
  if (result.error) throw result.error;
  assert.equal(result.status === 0, success, `${exe}: ${result.stdout}\n${result.stderr}`);
  return result.stdout + result.stderr;
}
assert.match(run(host, ['--test-collection', '--collection-dll', dll]), /Collection self-test passed/);
assert.match(run(host, ['--list-collections', '--collection-dll', dll]), /\bLADX2\b/);

const referenceFile = process.argv[3] ? path.resolve(process.argv[3]) : path.join(root,'efs/LADX2.efs');
const source=fs.readFileSync(referenceFile,'utf8');
console.log(`EFS reference: ${referenceFile}`);
const start=source.indexOf('class StudyCollection {');
const end=source.indexOf('class Strategy {',start);
const definitions=new vm.Script(source.slice(start,end)+'\nthis.collection=new StudyCollection(params);');
const calc=new vm.Script('collection.Calc();');
function reference(prices, params) {
  let index = 0;
  const at = (field, offset) => {
    const bar = prices[index + offset];
    if (!bar) return undefined;
    return field === 'ohlc4' ? (bar.open + bar.high + bar.low + bar.close) / 4 : bar[field];
  };
  const context = {params, BARSTATE_ALLBARS: 0, BARSTATE_NEWBAR: 1, BARSTATE_CURRENTBAR: 2,
    getInterval: () => params.chartInterval ?? 5, getValue: (field, offset) => at(field === 'rawtime' ? 'raw_time' : field, offset), getBarState: () => 1, getCurrentBarIndex: () => index, rawtime: () => at('raw_time', 0)};
  for (const field of ['open', 'high', 'low', 'close', 'ohlc4'])
    context[field] = offset => offset == null ? {getValue: n => at(field, n)} : at(field, offset);
  context.sma = period => ({getValue: offset => {
    if (index + offset < period - 1) return undefined;
    let sum = 0;
    for (let n = 0; n < period; n++) sum += at('close', offset - n);
    return sum / period;
  }});
  context.atr = period => {
    const values = [undefined];
    let sum = 0, previous;
    for (let n = 1; n < prices.length; n++) {
      const bar = prices[n], close = prices[n - 1].close;
      const tr = Math.max(Math.abs(bar.high - bar.low), Math.abs(bar.high - close), Math.abs(bar.low - close));
      if (n <= period) {
        sum += tr;
        if (n === period) previous = sum / period;
      } else previous = (previous * (period - 1) + tr) / period;
      values.push(previous);
    }
    return {getValue: offset => values[index + offset]};
  };
  const sandbox = vm.createContext(context);
  definitions.runInContext(sandbox, {timeout: 1000});
  const results = [];
  for (index = 0; index < prices.length; index++) {
    Object.assign(context, {cClose: at('close', 0), cHigh: at('high', 0), cLow: at('low', 0),
      pClose: at('close', -1), pHigh: at('high', -1), pLow: at('low', -1), ppClose: at('close', -2)});
    calc.runInContext(sandbox, {timeout: 1000});
    results.push({...sandbox.collection.studies[0]});
  }
  return results;
}
function cells(line) {
  const result = [];
  let value = '', quoted = false;
  for (let n = 0; n < line.length; n++) {
    const c = line[n];
    if (c === '"') {
      if (quoted && line[n + 1] === '"') { value += '"'; n++; }
      else quoted = !quoted;
    } else if (c === ',' && !quoted) { result.push(value); value = ''; }
    else value += c;
  }
  assert(!quoted, 'Unterminated CSV quote');
  result.push(value);
  return result;
}
function readOutput(file) {
  const lines = fs.readFileSync(file, 'utf8').trimEnd().split(/\r?\n/);
  const headers = cells(lines.shift());
  
  return lines.map(line => {
    const values = cells(line);
    assert.equal(values.length, headers.length);
    return Object.fromEntries(headers.map((name, index) => [name,
      (name.endsWith('Reason') || name.endsWith('TIMEFRAME')) ? values[index] : values[index] === '' ? null : Number(values[index])]));
  });
}
function prices(seed, count = 1200) {
  let ticks = 4000;
  const random = () => { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed; };
  return Array.from({length: count}, (_, index) => {
    const open = ticks / 4;
    ticks += (index % 240 < 120 ? 3 : -3) + random() % 17 - 8;
    if (index % 179 === 0) ticks -= 120;
    if (index % 227 === 0) ticks += 160;
    const close = ticks / 4;
    return {raw_time: 1609459200 + index * 300, open, close,
      high: Math.max(open, close) + (1 + random() % 16) / 4,
      low: Math.min(open, close) - (1 + random() % 16) / 4, volume: 100 + random() % 900};
  });
}
const ui=JSON.parse(fs.readFileSync(path.join(root,'ladx2-parameters.json'),'utf8'));
const two=JSON.parse(fs.readFileSync(path.join(root,'ladx2-2min-parameters.json'),'utf8'));
const fast={...ui,indicators:{...ui.indicators,adxLength:3,priceMaLength:2,guideMaLength:7,shockAtrLength:3},
 marketTrendSettings:{...ui.marketTrendSettings,smaLength:4,slopeLookback:2,dmiLength:3,adxLength:3,emaLength:3,atrLength:3}};
const scenarios=[['5min-defaults',ui,prices(2,6000)],['2min-preset',two,prices(3,9000).map((p,n)=>({...p,raw_time:1609459200+n*120}))],
 ['RUN0738',JSON.parse(fs.readFileSync(path.join(root,'ladx2-run0738-parameters.json'),'utf8')),prices(38,9000)],
 ['shock-exits',{...fast,entry:{...fast.entry,chopMinGuideWidthAtr:0},exit:{...fast.exit,shockDropAtr:0.5,shockRangeAtr:1,exitOnGuideCrossShortTrend:true}},prices(7,6000)],
 ['fast-hourly',fast,prices(4,6000)],['fast-daily',{...fast,marketTrendSettings:{...fast.marketTrendSettings,timeframe:'daily'}},prices(5,18000)],
 ['UTC',{...fast,marketTrendSettings:{...fast.marketTrendSettings,timeZone:'UTC'}},prices(6,6000)],
 ...['2024-03-08T13:15:00Z','2024-11-01T13:15:00Z'].flatMap((date,i)=>['hourly','daily'].map(timeframe=>[
   `DST-${i}-${timeframe}`, {...fast,marketTrendSettings:{...fast.marketTrendSettings,timeframe}},
   prices(10+i,10000).map((p,n)=>({...p,raw_time:Date.parse(date)/1000+n*300})).filter((p,n)=>n%311<290)]))];
let comparisons = 0;
const sides = new Set(), stops = new Set();
for (const [name, parameters, bars] of scenarios) {
  const parameterFile = path.join(outputRoot, name + '.json');
  const priceFile = path.join(outputRoot, name + '-prices.csv');
  const outputFile = path.join(outputRoot, name + '-values.csv');
  fs.writeFileSync(parameterFile, JSON.stringify(parameters));
  fs.writeFileSync(priceFile, 'RawTime,Open,High,Low,Close,Volume\n' + bars.map(p =>
    [p.raw_time, p.open, p.high, p.low, p.close, p.volume].join(',')).join('\n') + '\n');
  run(isolatedProbe, [isolatedDll, 'ladx2', parameterFile, priceFile, outputFile]);
  // Map the EFS strategy namespace to the new DLL's LADX2 outputs.
  const nativeName = key => key === 'STrend_LADX_Side' ? 'LADX2_SIDE' : key.replace(/^STrend_LADX_/, 'LADX2_');
  const actual = readOutput(outputFile), expected = reference(bars, {...parameters,chartInterval:name === '2min-preset' ? 2 : 5}).map(row =>
    Object.fromEntries(Object.entries(row).map(([key, value]) => [nativeName(key), value])));
  assert.equal(actual.length, expected.length);
  for (let n = 0; n < bars.length; n++) {
    for (const key of Object.keys(expected[n])) assert(Object.hasOwn(actual[n], key), `Missing EFS output ${key}`);
    for (const [key, got] of Object.entries(actual[n])) {
      if (key === 'RawTime') { assert.equal(got, bars[n].raw_time); continue; }
      let want = key === 'LADX2_LongSide' ? (expected[n].LADX2_SIDE > 0 ? 1 : 0) : expected[n][key];
      if (want == null || (typeof want === 'number' && !Number.isFinite(want))) want = null;
      const exact = key.endsWith('Side') || key.endsWith('SIDE') || key.endsWith('Reason') || key.endsWith('Chop') || key.endsWith('BarsSinceExit');
      const equal = typeof want !== 'number' || exact ? got === want :
        typeof got === 'number' && Math.abs(got - want) <= 2e-8 * (1 + Math.abs(want));
      assert(equal, `${name} bar ${n}, ${key}: native=${got}, EFS=${want}`);
      comparisons++;
    }
    sides.add(actual[n].LADX2_SIDE);
    stops.add(actual[n].LADX2_StopReason);
  }
  console.log(`${name}: ${bars.length} bars matched`);
}
assert(sides.has(1.1) && sides.has(1.2), 'Reference scenarios did not exercise both entry subtypes');
assert(stops.has('ADX_BIAS') && stops.has('SHOCK_DROP'), 'Reference scenarios did not exercise exits');
const invalid = path.join(outputRoot, 'invalid.json');
fs.writeFileSync(invalid, '{"AdxLenConf":3}');
assert.match(run(isolatedProbe, [isolatedDll, 'LADX2', invalid,
  path.join(outputRoot, 'fast-hourly-prices.csv'), path.join(outputRoot, 'invalid.csv')], false), /Unknown LADX2 parameter/);
assert(!fs.existsSync(path.join(outputRoot, 'invalid.csv')), 'Invalid parameters created an output file');
console.log(`LADX2 ${configuration}: ${comparisons} output comparisons passed; standalone deployment passed.`);
console.log(`Results: ${outputRoot}`);
