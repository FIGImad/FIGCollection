// Executable transcription of the user's Study_BaseSignal. Only the EFS host
// functions and bar inputs are supplied here; strategy methods are unchanged.
import fs from 'node:fs';
import { dirname } from 'node:path';
let cHigh, cLow;
function getBarState() { return 1; }
function PrepareStudyBar(study) {
    study.studyArr[1] = { ...study.studyArr[0] };
    study.studyArr[0] = { ...study.studyArr[1] };
}
class Study_BaseSignal {
    constructor(cycleTerm, baseStudies) {
        this.baseStudies = baseStudies;
        this.cycleTerm = cycleTerm;
        this.barStateDefaults = null;
        this.barStateCarryFields = null;
        this.studyArr = [{ Side: 0, postSide_H: 0, postSide_L: 0 }, {}];
        this.cycleTermStudy = this.baseStudies.GetStudy(this.cycleTerm);
    }
    get Side() { return this.studyArr[0].Side; }
    set Side(val) { this.studyArr[0].Side = val; }
    get postSide_H() { return this.studyArr[0].postSide_H; }
    set postSide_H(val) { this.studyArr[0].postSide_H = val; }
    get postSide_L() { return this.studyArr[0].postSide_L; }
    set postSide_L(val) { this.studyArr[0].postSide_L = val; }
    get ppostSide_H() { return this.studyArr[1].postSide_H; }
    set ppostSide_H(val) { this.studyArr[1].postSide_H = val; }
    get ppostSide_L() { return this.studyArr[1].postSide_L; }
    set ppostSide_L(val) { this.studyArr[1].postSide_L = val; }
    Calc(studies) {
        const barState = getBarState();
        PrepareStudyBar(this, barState, this.barStateDefaults, this.barStateCarryFields);
        const cStudy = studies[0];
        this.ResetSwitches(studies);
        if (this.Side === 0) this.Side = this.TestStart(studies);
        else this.Side = this.TestStop(studies) ? 0 : this.Side;
        cStudy[`${this.Tag('SIGNAL')}`] = this.Side;
        cStudy[`${this.Tag('postSide_H')}`] = this.postSide_H;
        cStudy[`${this.Tag('postSide_L')}`] = this.postSide_L;
    }
    ResetSwitches(studies) {
        this.postSide_H = this.postSide_H === 0 ? cHigh : this.postSide_H;
        this.postSide_L = this.postSide_L === 0 ? cLow : this.postSide_L;
        if (this.Side === 0) {
            this.postSide_H = cHigh;
            this.postSide_L = cLow;
        } else {
            this.postSide_H = cHigh > this.ppostSide_H ? cHigh : this.ppostSide_H;
            this.postSide_L = cLow < this.ppostSide_L ? cLow : this.ppostSide_L;
            if (this.postSide_H > this.ppostSide_H) this.postSide_L = cLow;
        }
    }
    TestStart(studies) {
        const cStudy = studies[0];
        if (cStudy == null) return 0;
        if (cLow > this.cycleTermStudy.MA && this.Side === 0) return 1;
        return 0;
    }
    TestStop(studies) {
        const cStudy = studies[0];
        if (cStudy == null) return true;
        const mat = cStudy[`${this.cycleTerm}_MAT`];
        return (this.Side !== 0 && cHigh < this.cycleTermStudy.MA && mat === -1 && cLow < this.ppostSide_L);
    }
    Tag(varName) { return Study_BaseSignal.Label(this.cycleTerm, varName); }
    static Label(cycleTerm, varName) { return `${cycleTerm}_${varName.toUpperCase()}`; }
}

const base = { MA: 0 };
const study = new Study_BaseSignal('L2', { GetStudy: () => base });
// Explicit equality boundaries, persistence, high resets and stop prerequisites.
const cases = [
    [102,100,100,0,0,102,100], [104,101,100,0,1,104,101],
    [103,99,100,1,1,104,99], [105,102,103,-1,1,105,102],
    [104,102,105,-1,1,105,102], [104,101,104,-1,1,105,101],
    [103,100,104,0,1,105,100], [102,99,104,-1,0,105,99],
    [100,98,99,-1,0,100,98], [103,101,100,1,1,103,101]
];
let seed = 12345;
function random(n) { seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0; return seed % n; }
for (let i = 0; i < 5000; i++) {
    const low = 80 + random(80) / 4;
    cases.push([low + random(40) / 4, low, 90 + random(60) / 4, random(3) - 1]);
}
const output = cases.map(([High, Low, MA, MAT, ...expected], i) => {
    cHigh = High; cLow = Low; base.MA = MA;
    study.Calc([{ L2_MAT: MAT }]);
    const actual = [study.Side, study.postSide_H, study.postSide_L];
    if (expected.length && JSON.stringify(expected) !== JSON.stringify(actual))
        throw new Error(`Manual boundary fixture ${i}: ${actual} != ${expected}`);
    return { High, Low, MA, MAT, Side: study.Side, PostHigh: study.postSide_H, PostLow: study.postSide_L };
});
const path = process.argv[2];
fs.mkdirSync(dirname(path), { recursive: true });
fs.writeFileSync(path, JSON.stringify(output));
console.log(`Generated ${output.length} EFS base signal reference bars.`);
