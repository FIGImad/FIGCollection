using FIGCommon.Exceptions;
using FIGCommon.Models;
using FIGCommon.Utilities;

namespace FIG.Studies
{
    public class StudyExtendedParams : BaseParams
    {
        public override void OnCheckpointRestored() => RecalcRegressionConstants();
        public int period = 0;
        public string source = "ohlc4";
        public double sumY = 0D;
        public double sumXY = 0D;
        public double sumYY = 0D;

        protected double sumX = 0D;
        protected double sumXX = 0D;
        protected double den = 0D;

        public StudyExtendedParams() : base() {
        }


        public StudyExtendedParams(int period, string source) : base()
        {
            this.period = period;
            this.source = source;
            if (period <= 0)
            {
                throw new Exception("Invalid period");
            }
            RecalcRegressionConstants();
        }

        public StudyExtendedParams(StudyExtendedParams paramLst) : base(paramLst)
        {
            Assign(paramLst);
        }

        public override void Assign(BaseParams obj)
        {
            base.Assign(obj);

            if (obj is not StudyExtendedParams)
            {
                throw new Exception("Invalid study params type");
            }

            var _params = (StudyExtendedParams)obj;
            sumY = _params.sumY;
            sumXY = _params.sumXY;
            sumYY = _params.sumYY;
            period = _params.period;
            source = _params.source;
            sumX = _params.sumX;
            sumXX = _params.sumXX;
            den = _params.den;
        }

        public override bool TryCopyForNextBar(BaseParams previous, FIGCommon.Models.PriceDataRS price)
        {
            if (GetType() != typeof(StudyExtendedParams) || previous.GetType() != typeof(StudyExtendedParams)) return false;
            var state = (StudyExtendedParams)previous;
            CopyBaseForNextBar(previous, price);
            sumY = state.sumY;
            sumXY = state.sumXY;
            sumYY = state.sumYY;
            period = state.period;
            source = state.source;
            sumX = state.sumX;
            sumXX = state.sumXX;
            den = state.den;
            return true;
        }

        public override BaseParams Clone()
        {
            // Scalar regression state and immutable source can be copied directly.
            // Price remains independent for correction replay and checkpoint history.
            var clone = (StudyExtendedParams)MemberwiseClone();
            clone.Price = new PriceDataRS(Price);
            return clone;
        }

        public void Calc(CPriceList priceList)
        {
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }

            // have enough price data to start the calculation
            if (priceList.Count < period)
            {
                ResetSums();
                return;
            }

            if (priceList.Count == period)
            {
                CalcInitial(priceList);
            }
            else
            {
                CalcUpdate(priceList);
            }
        }

        public void CalcInitial(CPriceList priceList)
        {
            ResetSums();

            if (priceList == null || priceList.Count < period)
            {
                return;
            }

            for (int i = 0; i < period; i++)
            {
                var priceItem = priceList.Get(-i);
                if (priceItem == null)
                {
                    ResetSums();
                    return;
                }

                double y = (double)SourceData.GetSourceDataItem(priceItem, source);
                double x = i + 1D;

                sumY += y;
                sumXY += x * y;
                sumYY += y * y;
            }
        }

        public void CalcUpdate(CPriceList priceList)
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

            double oldSumY = sumY;

            // Update Σxy using prior Σy
            // new Σxy = old Σxy - old* N + (Σy - old) + cur*1
            //         = old Σxy + Σy + cur - old*(N + 1)
            sumXY += oldSumY + cPrice - fPrice * (period + 1D);

            // Update Σy
            sumY += cPrice - fPrice;

            // Update Σy²
            sumYY += (cPrice * cPrice) - (fPrice * fPrice);
        }

        protected void RecalcRegressionConstants()
        {
            sumX = (double)period * (period + 1) / 2D; // Σx
            sumXX = (double)period * (period + 1) * (2D * period + 1D) / 6D; // Σx^2
            den = (double)period * sumXX - sumX * sumX; // N*Σx^2 - (Σx)^2
        }

        protected void ResetSums()
        {
            sumY = 0D;
            sumXY = 0D;
            sumYY = 0D;
        }

        public decimal? MA
        {
            get
            {
                if (period <= 0)
                {
                    return null;
                }

                return (decimal)(sumY / period);
            }
        }

        public decimal? LinearRegressionSlope
        {
            get
            {
                int n = period;

                if (n <= 0 || den == 0D)
                {
                    return null;
                }

                double b = ((double)n * sumXY - sumX * sumY) / den;
                return (decimal)b;
            }
        }

        public decimal? LinearRegressionIntercept
        {
            get
            {
                int n = period;

                if (n <= 0 || den == 0D)
                {
                    return null;
                }

                double b = ((double)n * sumXY - sumX * sumY) / den;
                double a = (sumY - b * sumX) / n;

                return (decimal)a;
            }
        }

        // What is previously marked as SEM
        public decimal? SEM
        {
            get
            {
                return LinearRegressionIntercept;
            }
        }

        public (decimal? LB, decimal? UB) GetBB(double stdDev)
        {
            if (period <= 0)
            {
                return (null, null);
            }

            double mean = sumY / period;
            double variance = (sumYY / period) - (mean * mean);

            // protect only against tiny negative floating-point drift
            variance = Math.Max(variance, 0D);

            double stdDevVal = Math.Sqrt(variance);

            decimal lb = (decimal)(mean - stdDev * stdDevVal);
            decimal ub = (decimal)(mean + stdDev * stdDevVal);

            return (lb, ub);
        }

    }


    public partial class StudyExtended : BaseStudy
    {
        protected override string? CheckpointConfiguration => GetType() == typeof(StudyExtended)
            ? System.Text.Json.JsonSerializer.Serialize(new { Publication = publication?.Signature, period, source, stdDev1, stdDev2, label }) : null;


        public PriceDataRS? price = null;
        public int period = 0;
        public string source = "ohlc4";
        public double stdDev1 = 1.0D;
        public double stdDev2 = 2.0D;
        public string label = "";
        private string? cachedLabel;
        private string[]? outputKeys;

        public StudyExtended(int period, string source, double stdDev1, double stdDev2, string label)
        {
            if (period <= 0)
            {
                throw new Exception("Invalid period");
            }

            this.period = period;
            this.source = source;
            this.stdDev1 = stdDev1;
            this.stdDev2 = stdDev2;
            this.label = label;
        }

        public StudyExtended(StudyExtended study)
        {
            Assign(study);
        }

        public override void Assign(BaseStudy obj)
        {
            base.Assign(obj);
            if (obj is not StudyExtended)
            {
                throw new Exception("Invalid study type");
            }

            var study = (StudyExtended)obj;
            publication = study.publication;
            requiredPublicationKeys = study.requiredPublicationKeys;
            publicationLabel = study.publicationLabel;
            price = study.price;
            period = study.period;
            source = study.source;
            stdDev1 = study.stdDev1;
            stdDev2 = study.stdDev2;
            label = study.label;
        }

        public StudyExtended Clone()
        {
            return new StudyExtended(this);
        }

        public override BaseParams? GetParams()
        {
            return new StudyExtendedParams(period, source);
        }

        public override string Tag(string? varName)
        {
            return StudyExtended.Label(label, varName);
        }
        public static string Label(string label, string? varName)
        {
            varName = varName == null ? "" : varName.ToUpper();
            // for backward compatability, we keep the same tag format as before
            return $"{varName}{label}";
        }
        protected void UpdateStudyValues(BarStudy? cStudy, StudyExtendedParams cParam)
        {
            if (cStudy == null) return;
            if (requiredPublicationKeys != null && publicationLabel != label)
                RefreshPublication();

            // label is public and mutable. Rebuild if it changes; preserve custom
            // Tag implementations in derived studies rather than caching them.
            string[]? keys = null;
            if (GetType() == typeof(StudyExtended))
            {
                if (outputKeys == null || cachedLabel != label)
                {
                    outputKeys = [Tag("MA"), Tag("SEM"), Tag("LB"), Tag("UB"), Tag("MID"), Tag("MID2"), Tag("LB2"), Tag("UB2")];
                    cachedLabel = label;
                }
                keys = outputKeys;
            }

            var (lb1, ub1) = cParam.GetBB(stdDev1);
            var (lb2, ub2) = cParam.GetBB(stdDev2);

            if (publication?.IsEnabled(0) != false) cStudy[publication?.Key(0) ?? (keys?[0] ?? Tag("MA"))] = cParam.MA;
            if (publication?.IsEnabled(1) != false) cStudy[publication?.Key(1) ?? (keys?[1] ?? Tag("SEM"))] = cParam.SEM;
            if (publication?.IsEnabled(2) != false) cStudy[publication?.Key(2) ?? (keys?[2] ?? Tag("LB"))] = lb1;
            if (publication?.IsEnabled(3) != false) cStudy[publication?.Key(3) ?? (keys?[3] ?? Tag("UB"))] = ub1;
            if (publication?.IsEnabled(4) != false) cStudy[publication?.Key(4) ?? (keys?[4] ?? Tag("MID"))] = cParam.MA;
            if (publication?.IsEnabled(5) != false) cStudy[publication?.Key(5) ?? (keys?[5] ?? Tag("MID2"))] = cParam.MA;
            if (publication?.IsEnabled(6) != false) cStudy[publication?.Key(6) ?? (keys?[6] ?? Tag("LB2"))] = lb2;
            if (publication?.IsEnabled(7) != false) cStudy[publication?.Key(7) ?? (keys?[7] ?? Tag("UB2"))] = ub2;
        }


        public override void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            var cParam = (StudyExtendedParams?)paramHist.Items[0];
            if (priceList == null || priceList.Count == 0)
            {
                throw new InvalidDataException("PriceList is empty or null");
            }
            if (cParam == null)
            {
                return;
            }
            cParam.Calc(priceList);
            BarStudy? cStudy = studies?.Items[0] ?? null;
            if (cStudy != null) 
            {
                UpdateStudyValues(cStudy, cParam);
            }
        }
    }
}

