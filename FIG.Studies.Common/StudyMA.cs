    using FIGCommon.Models;
using FIGCommon.Utilities;


namespace FIG.Studies
{
    public class StudyMAParams : BaseParams
    {
        public double runningSum = 0D;

        public StudyMAParams() : base()
        {
        }

        public StudyMAParams(StudyMAParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyMAParams)
            {
                throw new Exception("Invalid study params type");
            }
            
            var _params = (StudyMAParams)obj;
            runningSum = _params.runningSum;
        }

        public override BaseParams Clone()
        {
            return new StudyMAParams(this);
        }
    }

    public class StudyMA : BaseStudy
    {
        public int period = 0;
        public string source = "";
        public string label = "";
        public decimal? MA { get; set; } = null;

        public StudyMA(int period, string source, string label) : base()
        {
            this.period = period;
            this.source = source;
            this.label = label;
            MA = null;
            
            if (period <= 0)
            {
                throw new Exception("Invalid period");
            }
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudyMA)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyMA)obj;
            period = study.period;
            source = study.source;
            label = study.label;
            MA = study.MA;
        }

        public override BaseParams? GetParams()
        {
            return new StudyMAParams();
        }

        public override string Tag(string? _varName)
        {
            return StudyMA.Label(label);
        }

        public static string Label(string label)
        {
            return $"MA{label}";
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyMAParams?)paramHist.Items[0];

            // Typical Check
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
                return;
            }
            MA = null;

            // Start the calculation
            //
            // have enough price data to start the calculation
            if (priceList.Count < period)
            {
                cParam.runningSum = 0D;
            }
            else if (priceList.Count == period)
            {
                double sum = 0;
                for (int i = 0; i < period; i++)
                {
                    var price = priceList.Get(-i);
                    if (price != null)
                    {
                        sum += (double)SourceData.GetSourceDataItem(price, source); ;
                    }
                }
                cParam.runningSum = sum;
                MA = (decimal) (cParam.runningSum / (double) period);
            }
            else
            {
                var newPriceSet = priceList.Get(0);
                var firstPriceSet = priceList.Get(-period);
                if (newPriceSet != null && firstPriceSet != null)
                {
                    var cPrice = (double)SourceData.GetSourceDataItem(newPriceSet, source);
                    var fPrice = (double)SourceData.GetSourceDataItem(firstPriceSet, source);
                    cParam.runningSum = cParam.runningSum - fPrice + cPrice;
                    MA = (decimal)(cParam.runningSum / (double)period);
                }
                else
                {
                    throw new InvalidDataException("Invalid price data");
                }
            }
            BarStudy? cStudy = studies.Items[0];
            if (cStudy != null)
            {
                cStudy[Tag(null)] = MA;
            }
        }
    }
}

