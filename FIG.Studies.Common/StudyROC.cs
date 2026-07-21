using FIGCommon.Utilities;

namespace FIG.Studies
{

    public class StudyROCParams : BaseParams
    {
        public List<decimal> vals = new List<decimal>();

        public StudyROCParams() : base()
        {
        }

        public StudyROCParams(StudyROCParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyROCParams)
            {
                throw new Exception("Invalid study params type");
            }

            var _params = (StudyROCParams)obj;
            vals = new();
            _params.vals.ForEach(x => vals.Add(x));
        }

        public override BaseParams Clone()
        {
            return new StudyROCParams(this);
        }
    }


    public class StudyROC : BaseStudy
    {
        protected int length;
        protected string varName;

        public decimal? ROC { get; set; } = null;

        public StudyROC(int length, string varName)
        {
            this.length = length;
            this.varName = varName;
        }
        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudyMA)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyROC)obj;
            length = study.length;
            varName = study.varName;
            ROC = study.ROC;
        }

        public override BaseParams? GetParams()
        {
            return new StudyROCParams();
        }

        public override string Tag(string? _notUsed)
        {
            return StudyROC.Label(varName);
        }

        public static string Label(string varName)
        {
            return $"R{varName}";
        }

        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyROCParams?)paramHist.Items[0];

            // Typical Check
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
                return;
            }

            ROC = null;

            BarStudy? cStudy = studies.Items[0];
            BarStudy? pStudy = studies.Count > 1 ? studies.Items[1] : cStudy;
            BarStudy? ppStudy = studies.Count > 2 ? studies.Items[2] : pStudy;
            if (cStudy == null || pStudy == null || ppStudy == null)
            {
                return;
            }

            var val = (decimal) (cStudy[varName] ?? 0.0);

            cParam.vals.Add(val);
            if (cParam.vals.Count > length)
            {
                cParam.vals.RemoveAt(0);
            }
            decimal earliestVal = cParam.vals[0];
            decimal prevVal = cParam.vals[cParam.vals.Count - 1];
            decimal multiplier = StudyROC.ClosestPowerOfTen(val) / 10.0M;

            ROC = prevVal != 0 ? (earliestVal - prevVal) * multiplier / prevVal : 0;

            // studies is not used in this function except to update the values in case label is passed
            cStudy[Tag(null)] = ROC;
        }


        protected static decimal ClosestPowerOfTen(decimal num)
        {
            try
            {
                double power = Math.Ceiling(Math.Log10((double)num));
                return (decimal)Math.Pow(10, power);
            }
            catch
            {
                return 0;
            }
        }

    }
}

