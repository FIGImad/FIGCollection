"""Compare replay output to EFS trades without changing either source file."""
import csv, json, sys, bisect
from decimal import Decimal as D
from datetime import datetime, timezone
from zoneinfo import ZoneInfo
from collections import defaultdict

zone=ZoneInfo('America/New_York')
def read(p):
    with open(p,encoding='utf-8-sig',newline='') as f:
        return [{k.strip():v.strip() for k,v in r.items()} for r in csv.DictReader(f)]
def stamp(d,t):
    return int(datetime.strptime(d+' '+t.replace('a.m.','AM').replace('p.m.','PM'),'%Y-%m-%d %I:%M:%S %p').replace(tzinfo=zone).timestamp())
def row(n,en,ex,ep,xp,side): return dict(row=n,en=en,ex=ex,ep=D(ep),xp=D(xp),side=D(side).quantize(D('.0001')))
back=[row(i+2,int(r['EntryRawTime'])+300,int(r['ExitRawTime'])+300,r['EntryPrice'],r['ExitPrice'],r['Side']) for i,r in enumerate(read(sys.argv[1]))]
efs=[row(i+2,stamp(r['OpenDate'],r['OpenTime']),stamp(r['CloseDate'],r['CloseTime']),r['OpenPrice'],r['ClosePrice'],r['Side']) for i,r in enumerate(read(sys.argv[2]))]
first=min(r['en'] for r in efs); last=max(r['ex'] for r in efs)
back=[r for r in back if first<=r['en']<=last]
def key(r): return r['en'],r['ex'],r['ep'],r['xp']
lookup=defaultdict(list)
for r in back:lookup[key(r)].append(r)
exact=[]; missing=[]
for r in efs:
    if lookup[key(r)]:exact.append((lookup[key(r)].pop(0),r))
    else:missing.append(r)
extra=[x for values in lookup.values() for x in values]; timing=[]; only=[]
for r in missing:
    candidates=[x for x in extra if x['ep']==r['ep'] and x['xp']==r['xp'] and abs(x['en']-r['en'])<=3*86400 and abs(x['ex']-r['ex'])<=3*86400]
    if len(candidates)==1:
        x=candidates[0];extra.remove(x);timing.append((x,r))
    else:only.append(r)
bad=[(x,r) for x,r in exact+timing if x['side']!=r['side']]
summary=dict(backtest=len(back),efs=len(efs),exact_time_price=len(exact),timing_only=len(timing),matching_sides=len(exact)+len(timing)-len(bad),different_sides=len(bad),efs_only=len(only),backtest_only=len(extra))
summary['chronological_prices_and_sides_identical'] = len(back)==len(efs) and all((x['ep'],x['xp'],x['side'])==(y['ep'],y['xp'],y['side']) for x,y in zip(back,efs))
if len(sys.argv)>3:
    with open(sys.argv[3],newline='') as f: times=[int(r['RawTime']) for r in csv.DictReader(f)]
    checked=matched=0
    for x,y in timing:
        for field in ['en','ex']:
            if x[field]==y[field]: continue
            checked+=1; index=bisect.bisect_right(times,x[field]-300)
            matched+=index<len(times) and times[index]==y[field]
    summary['timing_events_checked']=checked
    summary['timing_events_at_next_available_price_bar']=matched
def show(r): return {**r,'en':datetime.fromtimestamp(r['en'],zone).isoformat(),'ex':datetime.fromtimestamp(r['ex'],zone).isoformat()}
print(json.dumps(dict(summary=summary,side_differences=[(show(x),show(y)) for x,y in bad],efs_only=[show(r) for r in only],backtest_only=[show(r) for r in extra],timing=[(show(x),show(y)) for x,y in timing]),default=str,indent=2))
