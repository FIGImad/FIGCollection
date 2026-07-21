using FIGCommon.Exceptions;
using FIGCommon.Utilities;

namespace FIG.Studies
{

    public class StudyBBParams : BaseParams
    {
        public double sumY;
        public double sumYY;

        public StudyBBParams() : base()
        {
        }

        public StudyBBParams(StudyBBParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyBBParams)
            {
                throw new Exception("Invalid study params type");
            }

            var _params = (StudyBBParams)obj;
            sumY = _params.sumY;
            sumYY = _params.sumYY;
        }

        public override BaseParams Clone()
        {
            return new StudyBBParams(this);
        }
    }
    public class StudyBB : BaseStudy
    {
        protected int period;
        protected double stdDev;
        protected string source;
        protected string label;
        protected string prefix;
        public decimal? MID { get; set; } = null;
        public decimal? UB { get; set; } = null;
        public decimal? LB { get; set; } = null;

        public StudyBB(int period, double stdDev, string source, string label, string prefix)
        {
            this.MID = null;
            this.UB = null;
            this.LB = null;

            this.period = period;
            this.stdDev = stdDev;
            this.source = source;
            this.label = label;
            this.prefix = prefix;

            if (period <= 0 || stdDev <= 0)
            {
                throw new Exception("Invalid param");
            }
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudyBB)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyBB)obj;
            period = study.period;
            stdDev = study.stdDev;
            source = study.source;
            label = study.label;
            prefix = study.prefix;
            MID = study.MID;
            UB = study.UB;
            LB = study.LB;
        }

        public override BaseParams? GetParams()
        {
            return new StudyBBParams();
        }

        public override string Tag(string? varName)
        {
            return StudyBB.Label(label, prefix, varName);
        }

        public static string Label(string label, string prefix, string? varName)
        {
            varName = varName == null ? "" : varName.ToUpper();
            prefix = prefix == "1" ? "" : prefix;
            if (varName == "UB" || varName == "LB" || varName == "MID")
            {
                return $"{varName}{prefix}{label}";
            }
            throw new VarNameNotSupportedException($"StudyBB: varName \"{(varName == null ? "NULL" : varName)}\" is not supported");
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyBBParams?)paramHist.Items[0];

            // Typical Check
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
                return;
            }

            // have enough price data to start the calculatio
            if (priceList.Count < period)
            {
                cParam.sumY = 0D;
                cParam.sumYY = 0D;
            }
            else if (priceList.Count == period)
            {
                cParam.sumY = 0;
                cParam.sumYY = 0;
                for (int i = 0; i < period; i++)
                {
                    var priceSet = priceList.Get(-i);
                    if (priceSet != null)
                    {
                        double price = (double)SourceData.GetSourceDataItem(priceSet, source);
                        cParam.sumY += price;
                        cParam.sumYY += price * price;
                    }
                }
            }
            else
            {
                var cPriceSet = priceList.Get(0);
                var fPriceSet = priceList.Get(-period);
                if (cPriceSet != null && fPriceSet != null)
                {
                    var cPrice = (double)SourceData.GetSourceDataItem(cPriceSet, source);
                    var fPrice = (double)SourceData.GetSourceDataItem(fPriceSet, source);

                    cParam.sumY = cParam.sumY - fPrice + cPrice;
                    cParam.sumYY = cParam.sumYY - (fPrice * fPrice) + (cPrice * cPrice);
                }
            }
            BBCalc(cParam);

            BarStudy? cStudy = studies.Items[0];
            if (cStudy != null)
            {
                cStudy[Tag("UB")] = this.UB;
                cStudy[Tag("LB")] = this.LB;
                cStudy[Tag("MID")] = this.MID;
            }
        }

        public void BBCalc(StudyBBParams cParam)
        {
            if (cParam.sumY == 0 || cParam.sumYY == 0)
            {
                MID = null;
                UB = null;
                LB = null;
                return;
            }
            double mean = cParam.sumY / (double)period;
            double variance = (cParam.sumYY / (double)period) - (mean * mean);
            double stdDevVal = Math.Sqrt((double)Math.Abs(variance));
            UB = (decimal)(mean + (stdDev * stdDevVal));
            LB = (decimal)(mean - (stdDev * stdDevVal));
            MID = (decimal)mean;
        }
    }
}

