using FIGCommon.Models;

namespace FIG.Studies
{
    public class BarStudy
    {
        public PriceDataRS Price { get; set; }
        public Dictionary<string, object?> Studies { get; set; }
        public BarStudy(PriceDataRS price)
        {
            Price = new PriceDataRS(price);
            Studies = new();
        }

        public BarStudy(BarStudy barStudy)
        {
            Price = new PriceDataRS(barStudy.Price);
            Studies = new();
            // iterate through Studies of barStudy and add to Studies
            foreach (var study in barStudy.Studies)
            {
                Studies.Add(study.Key, study.Value);
            }
        }

        // [] operator
        public object? this[string key]
        {
            get
            {
                // TryGetValue is more efficient than ContainsKey + indexer
                Studies.TryGetValue(key, out var value);
                return value;
            }
            set
            {
                // Direct assignment handles both cases (existing key and new key)
                Studies[key] = value;
            }
        }

        public bool ContainsKey(string v)
        {
            return Studies.ContainsKey(v);
        }
    }
}


