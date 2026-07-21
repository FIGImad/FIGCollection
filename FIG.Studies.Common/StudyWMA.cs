using FIGCommon.Models;
using FIGCommon.Utilities;


namespace FIG.Studies
{
    public class StudyWMAParams : BaseParams
    {
        public double weightedSum = 0D;
        public double sumDeltas = 0D;

        public StudyWMAParams() : base()
        {
        }

        public StudyWMAParams(StudyWMAParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyWMAParams)
            {
                throw new Exception("Invalid study params type");
            }

            var _params = (StudyWMAParams)obj;
            weightedSum = _params.weightedSum;
            sumDeltas = _params.sumDeltas;
        }

        public override BaseParams Clone()
        {
            return new StudyWMAParams(this);
        }
    }

    public class StudyWMA : BaseStudy
    {
        protected int period;
        protected string source;
        protected string label;
        public decimal? WMA { get; set; } = null;

        protected List<double> weights = new();
        public double sumW = 0D;
        protected double startW = 1D;
        protected double endW = 1D;
        protected double dw = 0D;

        public StudyWMA(int period, string source, string label)
        {
            this.period = period;
            this.source = source;
            this.label = label;
            if (period <= 0)
            {
                throw new Exception("Invalid period");
            }

            this.weights = new();
            this.sumW = 0D;
            this.startW = 1D;
            this.endW = (double)period;
            this.dw = (endW - startW) / (double)this.period;
            for (int i = 0; i < this.period; i++)
            {
                double cw = startW + (double)i * dw;
                weights.Add(cw);
                sumW += cw;
            }
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudyWMA)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyWMA)obj;
            period = study.period;
            source = study.source;
            label = study.label;
            WMA = study.WMA;
            weights = study.weights;
            sumW = study.sumW;
            startW = study.startW;
            endW = study.endW;
            dw = study.dw;
        }

        public override BaseParams? GetParams()
        {
            return new StudyWMAParams();
        }

        public override string Tag(string? _varName)
        {
            return StudyWMA.Label(label);
        }

        public static string Label(string label)
        {
            return $"WMA{label}";
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyWMAParams?)paramHist.Items[0];

            // Typical Check
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
                return;
            }

            WMA = null;

            // Start the calculation
            //
            // have enough price data to start the calculation
            if (priceList.Count < period)
            {
                cParam.weightedSum = 0D;
            }
            else if (priceList.Count == period)
            {
                CalcInitial(priceList, cParam);
            }
            else
            {
                CalcUpdate(priceList, cParam);
            }
            BarStudy? cStudy = studies.Items[0];
            if (cStudy != null)
            {
                cStudy[Tag(null)] = WMA;
            }
        }

        protected void CalcInitial(CPriceList priceList, StudyWMAParams cParam)
        {
            cParam.weightedSum = 0D;
            cParam.sumDeltas = 0D;
            double sum = 0D;
            if (priceList == null || priceList.Count == 0)
            {
                return;
            }

            for (int i = 0; i < priceList.Count; i++)
            {
                double cw = this.startW + (double)i * this.dw;
                var price = priceList.Get(-i);
                if (price == null) break;
                double y = (double)SourceData.GetSourceDataItem(price, source);
                sum += y * cw;
                cParam.sumDeltas += this.dw * y;
            }

            cParam.weightedSum = sum;
            this.WMA = (decimal) (cParam.weightedSum / this.sumW);
        }

        protected void CalcUpdate(CPriceList priceList, StudyWMAParams cParam)
        {
            this.WMA = null;
            if (priceList == null || priceList.Count == 0 || period <= 1)
            {
                return;
            }
            var newPriceSet = priceList.Get(0);
            var firstPriceSet = priceList.Get(-period);
            if (newPriceSet == null || firstPriceSet == null)
            {
                return;
            }

            double newPrice = (double)SourceData.GetSourceDataItem(newPriceSet, source);
            double oldPrice = (double)SourceData.GetSourceDataItem(firstPriceSet, source);

            double sumDeltas = cParam.sumDeltas - this.dw * oldPrice;
            double newVal = newPrice * this.weights[this.period - 1];
            double oldVal = oldPrice * this.weights[0] + sumDeltas;
            cParam.weightedSum = cParam.weightedSum - oldVal + newVal;
            cParam.sumDeltas = sumDeltas + newPrice * this.dw;
            this.WMA = (decimal) (cParam.weightedSum / this.sumW);
        }
    }
}

