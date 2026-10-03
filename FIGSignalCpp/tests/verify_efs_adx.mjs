// Reference calculation methods from the user-supplied EFS Study_ADX.
// Only the EFS price/time accessors are replaced here. This checks closed bars;
// native tests separately cover corrected same-bar seed handling and past-bar rewinds.
import fs from 'node:fs';
const [report, parameters] = process.argv.slice(2);
if (!report || !parameters) throw new Error('Usage: node tests/verify_efs_adx.mjs REPORT.csv PARAMETERS.json');
const lines = fs.readFileSync(report, 'utf8').trim().split(/\r?\n/);
const names = lines.shift().split(',');
const rows = lines.map(line => Object.fromEntries(line.split(',').map((v, i) => [names[i], v === '' ? undefined : Number(v)])));
const settings = JSON.parse(fs.readFileSync(parameters, 'utf8'));
let index = 0;
const series = name => ({getValue: offset => rows[index + offset]?.[name]});
const high = () => series('High'), low = () => series('Low'), close = () => series('Close');
const rawtime = () => rows[index].RawTime;
const getCurrentBarIndex = () => index;

class Study_ADX {
    constructor(length, smoothing) {
        this.length = length;
        this.smoothing = smoothing;
        this.highSeries = high(); this.lowSeries = low(); this.closeSeries = close();
        this.initDMFlag = false; this.initADXFlag = false;
        this.trSum = 0; this.pDMSum = 0; this.nDMSum = 0;
        this.baseTrSum = 0; this.basePDMSum = 0; this.baseNDMSum = 0;
        this.dxQueue = []; this.lastADX = undefined; this.baseADX = undefined;
        this.ADX = undefined; this.PDI = undefined; this.NDI = undefined;
        this.lastBarIndex = -1; this.lastRawTime = undefined; this.lastADXBarId = undefined;
    }
    Calc() {
        if (this.closeSeries.getValue(-1) == null) {
            this.ADX = undefined; this.PDI = undefined; this.NDI = undefined;
            return;
        }
        if (!this.initDMFlag) {
            if (this.closeSeries.getValue(-this.length) == null) {
                this.ADX = undefined; this.PDI = undefined; this.NDI = undefined;
                return;
            }
            this.CalcInitialDM(); this.CalcADXFromCurrentState(); return;
        }
        this.UpdateOptimized(); this.CalcADXFromCurrentState();
    }
    CalcInitialDM() {
        this.trSum = 0; this.pDMSum = 0; this.nDMSum = 0;
        for (let i = 0; i < this.length; i++) {
            const vals = this.CalcBarOffsetValues(i);
            this.trSum += vals.tr; this.pDMSum += vals.pDM; this.nDMSum += vals.nDM;
        }
        this.baseTrSum = this.trSum; this.basePDMSum = this.pDMSum; this.baseNDMSum = this.nDMSum;
        this.initDMFlag = true; this.lastBarIndex = getCurrentBarIndex(); this.lastRawTime = this.GetCurrentBarId();
    }
    UpdateOptimized() {
        const currentBarId = this.GetCurrentBarId();
        if (currentBarId !== this.lastRawTime) {
            this.baseTrSum = this.trSum; this.basePDMSum = this.pDMSum; this.baseNDMSum = this.nDMSum;
            if (this.initADXFlag) this.baseADX = this.lastADX;
            this.lastBarIndex = getCurrentBarIndex(); this.lastRawTime = currentBarId;
        }
        const vals = this.CalcBarOffsetValues(0);
        this.liveTr = vals.tr; this.livePDM = vals.pDM; this.liveNDM = vals.nDM;
        this.trSum = this.baseTrSum - (this.baseTrSum / this.length) + this.liveTr;
        this.pDMSum = this.basePDMSum - (this.basePDMSum / this.length) + this.livePDM;
        this.nDMSum = this.baseNDMSum - (this.baseNDMSum / this.length) + this.liveNDM;
    }
    CalcADXFromCurrentState() {
        if (this.trSum <= 0) {
            this.ADX = undefined; this.PDI = undefined; this.NDI = undefined; return;
        }
        const pdi = 100 * (this.pDMSum / this.trSum), ndi = 100 * (this.nDMSum / this.trSum);
        const diSum = pdi + ndi;
        let dx = 0;
        if (diSum > 0) dx = 100 * Math.abs(pdi - ndi) / diSum;
        this.PDI = pdi; this.NDI = ndi;
        if (!this.initADXFlag) {
            const currentBarId = this.GetCurrentBarId();
            if (this.dxQueue.length === 0 || currentBarId !== this.lastADXBarId) {
                this.dxQueue.unshift(dx);
                if (this.dxQueue.length > this.smoothing) this.dxQueue.pop();
                this.lastADXBarId = currentBarId;
            } else this.dxQueue[0] = dx;
            if (this.dxQueue.length < this.smoothing) { this.ADX = undefined; return; }
            let sum = 0;
            for (let i = 0; i < this.dxQueue.length; i++) sum += this.dxQueue[i];
            this.lastADX = sum / this.smoothing; this.baseADX = this.lastADX;
            this.ADX = this.lastADX; this.initADXFlag = true; return;
        }
        const seedADX = (this.baseADX == null) ? this.lastADX : this.baseADX;
        this.lastADX = ((seedADX * (this.smoothing - 1)) + dx) / this.smoothing;
        this.ADX = this.lastADX;
    }
    GetCurrentBarId() { return rawtime(0) ?? getCurrentBarIndex(); }
    CalcBarOffsetValues(i) {
        const currHigh = this.highSeries.getValue(-i), currLow = this.lowSeries.getValue(-i);
        const prevHigh = this.highSeries.getValue(-(i+1)), prevLow = this.lowSeries.getValue(-(i+1));
        const prevClose = this.closeSeries.getValue(-(i+1));
        const upMove = currHigh - prevHigh, downMove = prevLow - currLow;
        const pDM = (upMove > downMove && upMove > 0) ? upMove : 0;
        const nDM = (downMove > upMove && downMove > 0) ? downMove : 0;
        const tr = Math.max(currHigh - currLow, Math.max(Math.abs(currHigh - prevClose), Math.abs(currLow - prevClose)));
        return {tr, pDM, nDM};
    }
}

let checks = 0, maxDifference = 0;
function check(column, expected) {
    const actual = rows[index][column];
    const missing = v => v == null || Number.isNaN(v);
    if (missing(actual) !== missing(expected)) throw new Error(`Warmup mismatch: row ${index+1}, ${column}`);
    if (!missing(expected)) {
        const difference = Math.abs(actual-expected);
        if (!Number.isFinite(actual) || difference > 1e-8 * (1+Math.abs(expected)))
            throw new Error(`Mismatch: row ${index+1}, ${column}: ${actual} != ${expected}`);
        maxDifference = Math.max(maxDifference, difference);
    }
    checks++;
}
const active = new Study_ADX(settings.AdxLen ?? 50, settings.AdxLen ?? 50);
const conf = new Study_ADX(settings.AdxLenConf ?? 6, settings.AdxLenConf ?? 6);
const biasHistory = [];
const price = row => (row.Open+row.High+row.Low+row.Close)/4;
const average = (length, end=index, source=price) => end+1 < length ? undefined :
    rows.slice(end-length+1,end+1).reduce((sum,r) => sum+source(r),0)/length;
for (index=0; index<rows.length; index++) {
    active.Calc(); conf.Calc();
    for (const [label,study] of [['Active',active],['Conf',conf]]) {
        for (const key of ['ADX','PDI','NDI']) check(`${label}_${key}`,study[key]);
        check(`${label}_BIAS`,study.PDI-study.NDI);
    }
    const bias = active.PDI-active.NDI;
    if (Number.isFinite(bias)) biasHistory.unshift(bias);
    if (biasHistory.length>11) biasHistory.pop();
    const previous=biasHistory[1];
    check('Active_ADX_ROC',Number.isFinite(bias) && previous!=null ? bias-previous : undefined);
    check('Active_ADX_ROC_PERCENT',Number.isFinite(bias) && previous!=null && previous!==0 ? 100*(bias-previous)/previous : undefined);
    check('Active_ADX_SLOPE',Number.isFinite(bias) && biasHistory.length>10 ? (bias-biasHistory[10])/10 : undefined);
    check('MAPrice',average(settings.FastMALen ?? 4) ?? 0);
    check('MAStop',average(settings.SlowMALen ?? 4) ?? 0);
    const length=settings.GuideMALen ?? 8, mean=average(length);
    const squareMean=average(length,index,r => price(r)*price(r));
    const width=mean==null ? 0 : (settings.GuideStdDev ?? 0.6)*Math.sqrt(Math.max(0,squareMean-mean*mean));
    check('MIDGuide',mean ?? 0); check('UBGuide',mean==null ? 0 : mean+width); check('LBGuide',mean==null ? 0 : mean-width);
    for (const [label,n,back] of [['SLConf',(settings.FastMALen ?? 4)+6,settings.FastMALen ?? 4],['SLGuide',length,5]]) {
        const now=average(n,index,r => r.Close), past=average(n,index-back,r => r.Close);
        const slope=now==null || past==null ? undefined : (now-past)/back;
        check(`${label}_MA`,slope==null ? undefined : now);
        check(`${label}_MASlopeValue`,slope);
        check(`${label}_MASlope`,slope==null ? undefined : Math.abs(slope)<=0.1 ? 0 : slope>0 ? 1 : -1);
    }
}
console.log(`EFS reference passed: ${rows.length} bars, ${checks} values; largest absolute difference ${maxDifference}.`);
