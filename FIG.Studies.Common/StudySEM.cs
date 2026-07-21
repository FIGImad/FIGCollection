using FIGCommon.Models;
using FIGCommon.Utilities;


namespace FIG.Studies
{

    public class StudySEMParams : BaseParams
    {
        public double sumY = 0D;
        public double sumXY = 0D;

        public StudySEMParams() : base()
        {
        }

        public StudySEMParams(StudySEMParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudySEMParams)
            {
                throw new Exception("Invalid study params type");
            }

            var _params = (StudySEMParams)obj;
            sumY = _params.sumY;
            sumXY = _params.sumXY;
        }

        public override BaseParams Clone()
        {
            return new StudySEMParams(this);
        }
    }

    public class StudySEM : BaseStudy
    {
        protected int period;
        protected string source;
        protected string label;
        protected double sumX = 0D;
        protected double sumXX = 0D;
        protected double den = 0D;
        public decimal? SEM { get; set; } = null;

        public StudySEM(int period, string source, string label)
        {
            this.sumX = (double)period * (double)(period + 1) / (double)2;             // Σx
            this.sumXX = (double)((double)period * (double)(period + 1) * (double)(2 * period + 1)) / (double)6; // Σx^2
            // Denominator for slope: N*Σx^2 - (Σx)^2
            this.den = (double)period * this.sumXX - this.sumX * this.sumX;

            this.period = period;
            this.source = source;
            this.label = label;
            this.SEM = null;
            if (period <= 0 || this.den == 0D)
            {
                throw new Exception("Invalid period");
            }
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudySEM)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudySEM)obj;
            period = study.period;
            source = study.source;
            label = study.label;
            sumX = study.sumX;
            sumXX = study.sumXX;
            den = study.den;
            SEM = study.SEM;
        }

        public override BaseParams? GetParams()
        {
            return new StudySEMParams();
        }

        public override string Tag(string? _varName)
        {
            return StudySEM.Label(label);
        }

        public static string Label(string label)
        {
            return $"SEM{label}";
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudySEMParams?)paramHist.Items[0];

            // Typical Check
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
               return;
            }
            
            // have enough price data to start the calculation
            if (priceList.Count < period)
            {
                cParam.sumY = 0D;
                cParam.sumXY = 0D;
            }
            else if (priceList.Count == period)
            {
                CalcInitial(priceList, cParam);
            }
            else
            {
                CalcUpdate(priceList, cParam);
            }
            SEM = SEMCalc(cParam);
            BarStudy? cStudy = studies.Items[0];
            if (cStudy != null)
            {
                cStudy[Tag(null)] = SEM;
            }
        }

        protected void CalcInitial(CPriceList priceList, StudySEMParams cParam)
        {
            cParam.sumY = 0D;
            cParam.sumXY = 0D;
            SEM = 0;
            if (priceList == null || priceList.Count == 0)
            {
                return;
            }

            for (var i = 0; i < period; i++)
            {
                var price = priceList.Get(-i);
                //var priceNdx = priceList.Count - i - 1;
                if (price != null)
                {
                    double y = (double)SourceData.GetSourceDataItem(price, source);
                    double x = (double)i + 1D;
                    cParam.sumY += y;
                    cParam.sumXY += x * y;
                }
                else
                {
                    break;
                }
            }
        }

        protected void CalcUpdate(CPriceList priceList, StudySEMParams cParam)
        {
            if (priceList == null || priceList.Count == 0 || period < 1)
            {
                return;
            }
            var newPriceSet = priceList.Get(0);
            var firstPriceSet = priceList.Get(-period);
            if (newPriceSet == null || firstPriceSet == null)
            {
                return;
            }

            double cPrice = (double)SourceData.GetSourceDataItem(newPriceSet, source);
            double fPrice = (double)SourceData.GetSourceDataItem(firstPriceSet, source);

            double delta = cPrice - fPrice;

            // new Σxy = old Σxy - old* N + (Σy - old) + cur*1
            //         = old Σxy + Σy + cur - old*(N + 1)
            cParam.sumXY += cParam.sumY + cPrice - fPrice * (double)(period + 1);

            // Σy update
            cParam.sumY += delta;
        }

        public decimal? SEMCalc(StudySEMParams cParam)
        {
            double n = (double) period;
            double sumY = cParam.sumY;
            double sumXY = cParam.sumXY;

            if (sumY == 0 || sumXY == 0) return null;

            // slope b = (N*Σxy - Σx*Σy) / (N*Σx^2 - (Σx)^2)
            double b = ((double)n * sumXY - this.sumX * sumY) / this.den;

            // intercept a = (Σy - b*Σx) / N
            return (decimal)((sumY - b * this.sumX) / (double)n);
        }
    }
}