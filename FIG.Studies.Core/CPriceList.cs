using FIGCommon.Models;

namespace FIG.Studies
{
    public class CPriceList
    {
        int maxNumPrices;
        protected List<PriceDataRS> priceList;

        public CPriceList(int maxNumPrices = 400000) { 
            this.maxNumPrices = maxNumPrices;
            this.priceList = new List<PriceDataRS>();
        }

        public int Count
        {
            get
            {
                return priceList.Count;
            }
        }

        public void Add(PriceDataRS price)
        {
            // remove all studies from studies >= newPrice.RawTime
            while (priceList.Count > 0)
            {
                var studyRawTime = priceList[priceList.Count - 1].RawTime;
                if (studyRawTime >= price.RawTime)
                {
                    // remove last price in priceList
                    priceList.RemoveAt(priceList.Count - 1);
                    continue;
                }
                break;
            }
            priceList.Add(price);
            if (priceList.Count > maxNumPrices)
            {
                priceList.RemoveAt(0);
            }
        }

        public void Clear() { 
            this.priceList.Clear();
        }

        public PriceDataRS? Get(int pos) { 
            // if (pos == 0) ===> get last price
            // if (pos == -1) ===> get price before last 
            // etc.
            int index = this.priceList.Count - 1 + pos;

            if (index < 0 || index >= this.priceList.Count) 
            { 
                return null; 
            }
            return new PriceDataRS(this.priceList[index]);
        }

    }
}


