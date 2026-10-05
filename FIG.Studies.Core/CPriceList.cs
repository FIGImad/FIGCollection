using FIGCommon.Models;

namespace FIG.Studies
{
    public class CPriceList
    {
        int maxNumPrices;
        protected List<PriceDataRS> priceList;
        private List<PriceWindowDigest>? checkpointPrefixes;
        private PriceWindowDigest prefixBeforeFirst;

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
                    checkpointPrefixes?.RemoveAt(checkpointPrefixes.Count - 1);
                    continue;
                }
                break;
            }
            priceList.Add(price);
            if (checkpointPrefixes != null)
                checkpointPrefixes.Add((checkpointPrefixes.Count == 0 ? prefixBeforeFirst : checkpointPrefixes[^1])
                    ^ PriceWindowDigest.ForPrice(price));
            if (priceList.Count > maxNumPrices)
            {
                priceList.RemoveAt(0);
                if (checkpointPrefixes != null)
                {
                    prefixBeforeFirst = checkpointPrefixes[0];
                    checkpointPrefixes.RemoveAt(0);
                }
            }
        }

        public void Clear() { 
            this.priceList.Clear();
            checkpointPrefixes?.Clear();
            prefixBeforeFirst = default;
        }

        public List<PriceDataRS> CapturePrices() => priceList.Select(p => new PriceDataRS(p)).ToList();
        public List<PriceDataRS> CapturePrices(int count)
            => priceList.Skip(Math.Max(0, priceList.Count - count)).Select(p => new PriceDataRS(p)).ToList();

        public CheckpointPriceWindow CapturePriceWindow(int limit)
        {
            if (limit < 1 || priceList.Count == 0) throw new InvalidDataException("Cannot reference an empty price window.");
            if (checkpointPrefixes == null)
            {
                checkpointPrefixes = new List<PriceWindowDigest>(priceList.Count);
                var prefix = prefixBeforeFirst;
                foreach (var price in priceList)
                {
                    prefix ^= PriceWindowDigest.ForPrice(price);
                    checkpointPrefixes.Add(prefix);
                }
            }
            var start = Math.Max(0, priceList.Count - limit);
            var digest = checkpointPrefixes[^1] ^ (start == 0 ? prefixBeforeFirst : checkpointPrefixes[start - 1]);
            return new(CheckpointPriceWindow.CurrentAlgorithm, priceList.Count - start,
                priceList[start].RawTime, priceList[^1].RawTime, digest.ToString());
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


