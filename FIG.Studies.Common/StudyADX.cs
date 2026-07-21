using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Utilities;

namespace FIG.Studies
{
    public class StudyADXParams : BaseParams
    {
        public double trSum;
        public double pDMSum;
        public double nDMSum;

        public double baseTRSum;
        public double basePDMSum;
        public double baseNDMSum;
        public bool hasBaseDMSum;
        public long lastDMRawTime = -1;
        public long dmInitializedRawTime = -1;

        public double dxSum;
        public int dxCount;
        public double lastDX;
        public long lastDXRawTime = -1;

        public bool dmInitialized;
        public bool adxInitialized;
        public long adxInitializedRawTime = -1;

        public decimal? lastADX;
        public decimal? baseADX;
        public long lastADXRawTime = -1;

        public StudyADXParams() : base()
        {
        }

        public StudyADXParams(StudyADXParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyADXParams)
            {
                throw new Exception("Invalid study params type");
            }

            var p = (StudyADXParams)obj;

            trSum = p.trSum;
            pDMSum = p.pDMSum;
            nDMSum = p.nDMSum;

            baseTRSum = p.baseTRSum;
            basePDMSum = p.basePDMSum;
            baseNDMSum = p.baseNDMSum;
            hasBaseDMSum = p.hasBaseDMSum;
            lastDMRawTime = p.lastDMRawTime;
            dmInitializedRawTime = p.dmInitializedRawTime;

            dxSum = p.dxSum;
            dxCount = p.dxCount;
            lastDX = p.lastDX;
            lastDXRawTime = p.lastDXRawTime;

            dmInitialized = p.dmInitialized;
            adxInitialized = p.adxInitialized;
            adxInitializedRawTime = p.adxInitializedRawTime;

            lastADX = p.lastADX;
            baseADX = p.baseADX;

            lastADXRawTime = p.lastADXRawTime;
        }

        public override BaseParams Clone()
        {
            return new StudyADXParams(this);
        }
    }

    public class StudyADX : BaseStudy
    {
        protected int length;
        protected int smoothing;
        protected string label;

        public decimal? ADX { get; set; } = null;
        public decimal? PDI { get; set; } = null;
        public decimal? NDI { get; set; } = null;

        public StudyADX(
            int length,
            int smoothing,
            string label)
        {
            ADX = null;
            PDI = null;
            NDI = null;

            this.length = length;
            this.smoothing = smoothing;
            this.label = label;

            if (length <= 0 || smoothing <= 0)
            {
                throw new Exception("Invalid param");
            }
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);

            if (obj is not StudyADX)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyADX)obj;

            length = study.length;
            smoothing = study.smoothing;
            label = study.label;

            ADX = study.ADX;
            PDI = study.PDI;
            NDI = study.NDI;
        }

        public override BaseParams? GetParams()
        {
            return new StudyADXParams();
        }

        public override string Tag(string? varName)
        {
            return StudyADX.Label(label, varName);
        }

        public static string Label(string label, string? varName)
        {
            varName = varName == null ? "" : varName.ToUpper();

            if (varName == "ADX" || varName == "PDI" || varName == "NDI")
            {
                return $"{label}_{varName}";
            }

            throw new VarNameNotSupportedException(
                $"StudyADX: varName \"{(varName == null ? "NULL" : varName)}\" is not supported");
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyADXParams?)paramHist.Items[0];

            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }

            if (cParam == null)
            {
                return;
            }

            // Need at least 2 bars to compute TR / DM.
            if (priceList.Count < 2)
            {
                ADX = null;
                PDI = null;
                NDI = null;
                WriteToStudy(studies);
                return;
            }

            long currentRawTime = cParam.Price.RawTime;

            bool needsInitialDMSums =
                !cParam.dmInitialized ||
                (cParam.dmInitializedRawTime == currentRawTime && !cParam.hasBaseDMSum);

            if (needsInitialDMSums)
            {
                if (priceList.Count < length + 1)
                {
                    ADX = null;
                    PDI = null;
                    NDI = null;
                    WriteToStudy(studies);
                    return;
                }

                cParam.trSum = 0D;
                cParam.pDMSum = 0D;
                cParam.nDMSum = 0D;
                cParam.hasBaseDMSum = false;

                for (int i = 0; i < length; i++)
                {
                    var curr = priceList.Get(-i);
                    var prev = priceList.Get(-(i + 1));

                    if (curr == null || prev == null)
                    {
                        continue;
                    }

                    CalcDMTR(curr, prev, out double tr, out double pDM, out double nDM);

                    cParam.trSum += tr;
                    cParam.pDMSum += pDM;
                    cParam.nDMSum += nDM;
                }

                cParam.dmInitialized = true;
                cParam.dmInitializedRawTime = currentRawTime;
                cParam.lastDMRawTime = currentRawTime;
            }
            else
            {
                var curr = priceList.Get(0);
                var prev = priceList.Get(-1);

                if (curr != null && prev != null)
                {
                    CalcDMTR(curr, prev, out double tr, out double pDM, out double nDM);

                    double seedTRSum = cParam.trSum;
                    double seedPDMSum = cParam.pDMSum;
                    double seedNDMSum = cParam.nDMSum;

                    if (cParam.lastDMRawTime == currentRawTime && cParam.hasBaseDMSum)
                    {
                        seedTRSum = cParam.baseTRSum;
                        seedPDMSum = cParam.basePDMSum;
                        seedNDMSum = cParam.baseNDMSum;
                    }

                    cParam.baseTRSum = seedTRSum;
                    cParam.basePDMSum = seedPDMSum;
                    cParam.baseNDMSum = seedNDMSum;
                    cParam.hasBaseDMSum = true;

                    cParam.trSum = seedTRSum - (seedTRSum / length) + tr;
                    cParam.pDMSum = seedPDMSum - (seedPDMSum / length) + pDM;
                    cParam.nDMSum = seedNDMSum - (seedNDMSum / length) + nDM;
                    cParam.lastDMRawTime = currentRawTime;
                }
            }

            ADXCalc(cParam);
            WriteToStudy(studies);
        }

        protected void WriteToStudy(CircularBuffer<BarStudy> studies)
        {
            BarStudy? cStudy = studies.Items[0];

            if (cStudy != null)
            {
                cStudy[Tag("ADX")] = ADX;
                cStudy[Tag("PDI")] = PDI;
                cStudy[Tag("NDI")] = NDI;
            }
        }

        protected void ADXCalc(StudyADXParams cParam)
        {
            if (cParam.trSum <= 0)
            {
                ADX = null;
                PDI = null;
                NDI = null;
                return;
            }

            double pdi = 100.0 * (cParam.pDMSum / cParam.trSum);
            double ndi = 100.0 * (cParam.nDMSum / cParam.trSum);
            double diSum = pdi + ndi;

            double dx = 0D;

            if (diSum > 0)
            {
                dx = 100.0 * Math.Abs(pdi - ndi) / diSum;
            }

            PDI = (decimal)pdi;
            NDI = (decimal)ndi;

            long currentRawTime = cParam.Price.RawTime;

            bool needsInitialADX =
                !cParam.adxInitialized ||
                cParam.adxInitializedRawTime == currentRawTime;

            if (needsInitialADX)
            {
                if (currentRawTime == cParam.lastDXRawTime)
                {
                    cParam.dxSum = cParam.dxSum - cParam.lastDX + dx;
                }
                else
                {
                    cParam.dxSum += dx;
                    cParam.dxCount++;
                }

                cParam.lastDX = dx;
                cParam.lastDXRawTime = currentRawTime;

                if (cParam.dxCount < smoothing)
                {
                    ADX = null;
                    return;
                }

                double firstAdx = cParam.dxSum / smoothing;

                cParam.lastADX = (decimal)firstAdx;
                cParam.baseADX = null;
                cParam.adxInitialized = true;
                cParam.adxInitializedRawTime = currentRawTime;
                cParam.lastADXRawTime = currentRawTime;

                ADX = cParam.lastADX;
                return;
            }

            decimal? seedADX = cParam.lastADX;
            if (cParam.lastADXRawTime == currentRawTime)
            {
                seedADX = cParam.baseADX;
            }

            if (seedADX == null)
            {
                ADX = null;
                return;
            }

            double nextAdx = (((double)seedADX.Value * (smoothing - 1)) + dx) / smoothing;

            cParam.lastADX = (decimal)nextAdx;
            cParam.baseADX = seedADX;
            cParam.lastADXRawTime = currentRawTime;

            ADX = cParam.lastADX;
        }

        protected void CalcDMTR(
            PriceDataRS currBar,
            PriceDataRS prevBar,
            out double tr,
            out double pDM,
            out double nDM)
        {
            double currHigh = (double)currBar.High;
            double currLow = (double)currBar.Low;

            double prevHigh = (double)prevBar.High;
            double prevLow = (double)prevBar.Low;
            double prevClose = (double)prevBar.Close;

            double upMove = currHigh - prevHigh;
            double downMove = prevLow - currLow;

            pDM = (upMove > downMove && upMove > 0D) ? upMove : 0D;
            nDM = (downMove > upMove && downMove > 0D) ? downMove : 0D;

            double range1 = currHigh - currLow;
            double range2 = Math.Abs(currHigh - prevClose);
            double range3 = Math.Abs(currLow - prevClose);

            tr = Math.Max(range1, Math.Max(range2, range3));
        }
    }
}


