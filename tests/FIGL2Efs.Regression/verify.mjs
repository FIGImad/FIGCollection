import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { spawnSync } from 'node:child_process';

const here = path.dirname(fileURLToPath(import.meta.url));
const source = fs.readFileSync(path.resolve(here, '../../../FIGSignalCollection/FIG.StudyCollections.L2/L2_V3_Oct14.efs'), 'utf8');
const start = source.indexOf('class Study_Base_L1');
const end = source.indexOf('\nclass ', start + 1);
const ctx = vm.createContext({ Study_ADX: { Label: () => 'ADX' } });
vm.runInContext(source.slice(start, end) + '\nthis.ReferenceStudy = Study_Base_L1;', ctx);
const zero = new Proxy({}, { get: () => 0 });
const fields = [...source.slice(start, source.indexOf('this.signal =', start)).matchAll(/\b(\w+)\s*:\s*0\b/g)].map(m => m[1]);
const triggers = [...new Set([...source.slice(source.indexOf('    TestStart(studies)', start), source.indexOf('    TestStop(studies)', start)).matchAll(/this\.trigger_DL1\s*===\s*([\d.]+)/g)].map(m => Number(m[1])))];
const cases = [];
for (const profile of [0,1,2]) {
for (const basic of [0, 7.74, 7.34]) for (const side of [0, 7.7498, -4.8549]) for (const price of [-1, 0, 100])
    cases.push({method: 'ResetSwitches', trigger: 0, basic, side, price,profile});
for (const trigger of triggers) for (const price of [-1, 0, 100]) cases.push({method: 'TestStart', trigger, basic: 0, side: 0, price,profile});
for (const side of triggers.flatMap(t => [t, -t])) for (const price of [-1, 0, 100]) cases.push({method:'TestStop', trigger:0, basic:0, side, price,profile});
}
const expected = cases.map(f => {
    const study = new ctx.ReferenceStudy('C21', {GetStudy: cycle => new Proxy({}, {get: (_,field) => {
        field = String(field).replace(/^pp?/, '');
        if (f.profile === 0 || !['MA','SEM','UB1','LB1','UB2','LB2'].includes(field)) return 0;
        let hash=0; for (const c of cycle+field) hash=(hash*31+c.charCodeAt(0))%997;
        return 50+((hash+f.profile*73)%400)/4;
    }})});
    const state = Object.fromEntries(fields.map(k => [k, 0]));
    Object.assign(state, {trigger_DL1: f.trigger, basicTrigger_DL1: f.basic, Side:f.side});
    study.studyArr = [{...state},{...state},{...state}];
    Object.assign(study, {LowerMAs:0, ActiveSEMTrend:0, L2_MAC_Pivot_Trend:0});
    Object.assign(ctx, {cClose:f.price,cHigh:f.price,cLow:f.price,pClose:f.price,pHigh:f.price,pLow:f.price});
    const result = study[f.method]([zero,zero,zero]);
    return {result: f.method === 'ResetSwitches' ? 0 : Number(result), basic:study.basicTrigger_DL1,trigger:study.trigger_DL1,firsttime:study.firsttime,side:study.Side};
});
const out = path.join(here,'obj','parity'); fs.mkdirSync(out,{recursive:true});
fs.writeFileSync(path.join(out,'cases.json'),JSON.stringify(cases));
const run = spawnSync('dotnet',[path.join(here,'bin/Release/net10.0/FIGL2Efs.Regression.dll'),path.join(out,'cases.json'),path.join(out,'actual.json')],{encoding:'utf8'});
assert.equal(run.status,0,run.stderr || run.stdout);
const actual = JSON.parse(fs.readFileSync(path.join(out,'actual.json'),'utf8'));
const differences = [];
for (let i=0;i<cases.length;i++) for (const key of Object.keys(expected[i]))
    if (expected[i][key] !== actual[i][key]) differences.push({fixture:cases[i],field:key,efs:expected[i][key],dotnet:actual[i][key]});
fs.writeFileSync(path.join(out,'differences.json'),JSON.stringify(differences,null,2));
console.log(JSON.stringify({cases:cases.length,differences:differences.length,examples:differences.slice(0,12)},null,2));
assert.equal(differences.length,0,'EFS strategy fixture discrepancies');
