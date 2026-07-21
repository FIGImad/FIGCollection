using FIGCommon.Models;
using FIGCommon.Utilities;


namespace FIG.Studies
{
    public class BaseStudy 
    {
        private readonly int MAXBUFFER = 10;
        protected CircularBuffer<BaseParams> paramHist;


        public BaseStudy()
        {
             paramHist = new CircularBuffer<BaseParams>(MAXBUFFER);
        }

        public BaseStudy(BaseStudy study)
        {
            paramHist = new CircularBuffer<BaseParams>(MAXBUFFER);
            Assign(study);
        }


        public virtual void Assign(BaseStudy study)
        {
            paramHist.Clear();
            foreach (var item in study.paramHist.Items)
            {
                paramHist.AddToFront(item?.Clone());
            }
        }

        public void Process(PriceDataRS price, CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            // get the param from previous price
            while (paramHist.Items[0] != null && paramHist.Items[0]?.Price.RawTime >= price.RawTime)
            {
                paramHist.RemoveAt(0);
            }
            var cParam = paramHist.Items[0]?.Clone()?? GetParams();
            paramHist.AddToFront(cParam);
            if (cParam != null)
            {
                cParam.Price = new PriceDataRS(price);
            }
            Calc(studies, priceList);
        }

        public virtual void Calc(CircularBuffer<BarStudy> studies, CPriceList priceList)
        {
            throw new NotImplementedException();
        }

        public virtual BaseParams? GetParams()
        {
            throw new NotImplementedException();
        }

        public virtual string Tag(string? varName = null)
        {
            throw new NotImplementedException();
        }

    }
}

