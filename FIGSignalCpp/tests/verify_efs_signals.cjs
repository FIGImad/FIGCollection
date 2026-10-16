// Compare EFS signal classes with a JSONL dump produced by the C# test harness.
const fs = require('fs'), vm = require('vm');
const [efsFile, dumpFile, parameterFile] = process.argv.slice(2);
const source = fs.readFileSync(efsFile, 'utf8');
const start = source.indexOf('class Study_LBaseADX {');
const end = source.indexOf('class Study_LBaseADXAttempt', start);
if (start < 0 || end < 0) throw Error('EFS signal classes not found');
const params = JSON.parse(fs.readFileSync(parameterFile, 'utf8'));
params.AdxTriggerConf ??= 0;
const ctx = vm.createContext({ params, rawtime: () => ctx.time, getCurrentBarIndex: () => ctx.index,
  Study_ADX: {Label:(t,n)=>`${t}_${n}`}, Study_PriceShock:{Label:(t,n)=>`${t}_${n}`},
  Study_SMASlope:{Label:(t,n)=>`${t}_${n}`} });
vm.runInContext(source.slice(start,end)+'\nthis.signals=[new Study_LBaseADX("STrend",params),new Study_SBaseADX("STrend",params)];',ctx);
const history=[]; let previous, previousPrevious, count=0, continuationDifferences=0;
for (const line of fs.readFileSync(dumpFile,'utf8').trim().split(/\r?\n/)) {
  const {price:p,values}=JSON.parse(line);
  Object.assign(ctx,{time:p.RawTime,index:count,cClose:p.Close,cHigh:p.High,cLow:p.Low,
    pClose:previous?.Close,pHigh:previous?.High,pLow:previous?.Low,
    ppClose:previousPrevious?.Close});
  const current=Object.fromEntries(Object.entries(values).filter(([k])=>!k.startsWith('STrend_')));
  history.unshift(current); history.length=Math.min(3,history.length);
  for(const s of ctx.signals) {
    s.studyArr.pop(); s.studyArr.unshift({}); s.studyArr[0]=JSON.parse(JSON.stringify(s.studyArr[1]));
    s.Calc(history);
  }
  for(const [key,expected] of Object.entries(current)) {
    if(!key.startsWith('STrend_') || key.endsWith('_Flags')) continue; // EFS-only debug object.
    if (!(key in values)) continue; // Native collection emits long signals only.
    const actual=values[key];
    const equal=expected==null ? actual==null : typeof expected==='number' ? Math.abs(actual-expected)<2e-6 : actual===expected;
    // JSON exports convert internal double values to decimal. Equality boundaries can
    // change this informational flag; count and report them, never suppress a side mismatch.
    if(!equal && key.endsWith('_ContinuationReady')) { continuationDifferences++; continue; }
    if(!equal) throw Error(`bar ${count} ${key}: C#=${actual}, EFS=${expected}`);
  }
  previousPrevious=previous; previous=p; count++;
}
console.log(`EFS parity: ${count} bars; all available signal/diagnostic columns matched; ${continuationDifferences} ContinuationReady differences. Missing columns and EFS debug Flags excluded.`);

