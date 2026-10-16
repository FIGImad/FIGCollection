import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
import assert from 'node:assert/strict';
const root = path.dirname(fileURLToPath(import.meta.url));
const source = fs.readFileSync(path.join(root, 'Reference.efs'), 'utf8');
const out = path.join(root, 'obj', 'efs-parity'); fs.mkdirSync(out, {recursive:true});
const dll = path.join(root, 'bin/Release/net10.0/FIGLADXEfs.Regression.dll');
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

const ui = JSON.parse(source.match(/const RUN0193_PARAMETERS\s*=\s*(\{[\s\S]*?\n\})/)[1]);
const run738 = JSON.parse(source.match(/const RUN0738_PARAMETERS\s*=\s*(\{[\s\S]*?\n\})/)[1]);
const fast={...ui,indicators:{...ui.indicators,adxLength:3,priceMaLength:2,guideMaLength:7,shockAtrLength:3},
 marketTrendSettings:{...ui.marketTrendSettings,smaLength:4,slopeLookback:2,dmiLength:3,adxLength:3,emaLength:3,atrLength:3}};
const scenarios = [
 ['RUN0193',ui,prices(2,6000)], ['RUN0738',run738,prices(38,6000)],
 ['fast-hourly',fast,prices(4,6000)],
 ['2min',{...fast,chartInterval:2},prices(3,6000).map((b,n)=>({...b,raw_time:1609459200+n*120}))],
 ['armed',{...fast,exit:{...fast.exit,stopBiasLongTrend:-100,stopBiasShortTrend:-100,shockDropAtr:100,shockRangeAtr:100,shockArmUpAtr:.1,shockArmedDropAtr:.2}},prices(17,3000)],
 ['guide-cross',{...fast,entry:{...fast.entry,startBiasLongTrend:-10,startBiasShortTrend:-10,chopMinGuideWidthAtr:0},exit:{stopBiasLongTrend:-100,stopBiasShortTrend:-100,shockDropAtr:100,shockRangeAtr:100,shockArmedDropAtr:100,shockArmUpAtr:100,exitOnGuideCrossShortTrend:true}},prices(7,6000)],
 ['shock-exits',{...fast,entry:{...fast.entry,chopMinGuideWidthAtr:0},exit:{...fast.exit,shockDropAtr:.5,shockRangeAtr:1,exitOnGuideCrossShortTrend:true}},prices(9,6000)],
 ['fast-daily',{...fast,marketTrendSettings:{...fast.marketTrendSettings,timeframe:'daily'}},prices(5,18000)],
 ['flat',fast,prices(6,1000).map(b=>({...b,open:100,high:100,low:100,close:100}))],
 ...['2024-03-08T13:15:00Z','2024-11-01T13:15:00Z'].flatMap((date,i)=>['hourly','daily'].map(timeframe=>[
 `DST-${i}-${timeframe}`,{...fast,marketTrendSettings:{...fast.marketTrendSettings,timeframe}},prices(10+i,10000).map((p,n)=>({...p,raw_time:Date.parse(date)/1000+n*300})).filter((p,n)=>n%311<290)])),
 ['UTC',{...fast,marketTrendSettings:{...fast.marketTrendSettings,timeZone:'UTC'}},prices(6,6000)]
];
let comparisons=0; const sides=new Set(), stops=new Set();
for (const [name,parameters,bars] of scenarios) {
 const paramFile=path.join(out,'params.json'), inputFile=path.join(out,'bars.json'), outputFile=path.join(out,'outputs.json');
 fs.writeFileSync(paramFile,JSON.stringify(Object.fromEntries(Object.entries(parameters).filter(([key])=>key!=='chartInterval'))));
 fs.writeFileSync(inputFile,JSON.stringify(bars.map(b=>({RawTime:b.raw_time,Open:b.open,High:b.high,Low:b.low,Close:b.close}))));
 const run=spawnSync('dotnet',[dll,paramFile,inputFile,outputFile],{encoding:'utf8',windowsHide:true});
 assert.equal(run.status,0,run.stdout+'\n'+run.stderr);
 const actual=JSON.parse(fs.readFileSync(outputFile,'utf8'));
 const expected=reference(bars,parameters).map(row=>Object.fromEntries(Object.entries(row).map(([key,value])=>[key==='STrend_LADX_Side'?'LADX_SIDE':key.replace(/^STrend_LADX_/,'LADX_'),value])));
 for(let n=0;n<bars.length;n++) {
  for(const key of Object.keys(expected[n])) assert(Object.hasOwn(actual[n],key),`Missing ${key}`);
  for(const [key,got] of Object.entries(actual[n])) {
   let want=key==='LADX_LongSide'?(expected[n].LADX_SIDE>0?1:0):expected[n][key];
   if(want==null || (typeof want==='number'&&!Number.isFinite(want))) want=null;
   const exact=/Side$|SIDE$|Reason$|Chop$|BarsSinceExit$|MarketTrend$|READY$/.test(key);
   assert(typeof want!=='number'||exact?got===want:typeof got==='number'&&Math.abs(got-want)<=2e-8*(1+Math.abs(want)),`${name} bar ${n} ${key}: C#=${got} EFS=${want}`);
   comparisons++;
  }
  sides.add(actual[n].LADX_SIDE); stops.add(actual[n].LADX_StopReason);
 }
 console.log(`${name}: ${bars.length} bars matched`);
}
assert(sides.has(1.1)&&sides.has(1.2),'Both entry types covered');
assert(stops.has('SHORT_TREND_GUIDE_CROSS'),'Guide cross covered');
assert(stops.has('ARMED_DROP'),'Armed exit covered');
console.log(`${comparisons} EFS comparisons passed. Exit reasons: ${[...stops].join(', ')}`);

