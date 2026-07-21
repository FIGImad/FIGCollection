namespace FIG.Studies
{
    public class Periods
    {
        public Dictionary<string, int> mapPeriods;
        public List<string> lengthSeq;

        public Periods(int c21)
        {
            mapPeriods = new Dictionary<string, int>();

            int C21 = c21;
            int C1 = (int)Math.Round(C21 * 0.055);
            int C2 = (int)Math.Round(C21 * 0.09);
            int C3 = (int)Math.Round(C21 * 0.146);
            int C5 = (int)Math.Round(C21 * 0.236);
            int C8 = (int)Math.Round(C21 * 0.382);
            int C13 = (int)Math.Round(C21 * 0.618);
            int C34 = (int)Math.Round(C21 * 1.618);
            int C55 = (int)Math.Round(C21 * 2.618);
            int C89 = (int)Math.Round(C21 * 4.236);
            int C144 = (int)Math.Round(C21 * 6.857);
            int C233 = (int)Math.Round(C21 * 11.095);
            int C377 = (int)Math.Round(C21 * 17.942);
            int C610 = (int)Math.Round(C21 * 29.05);

            int L1 = (int)Math.Round(C21 * 0.034);
            int L2 = (int)Math.Round(C21 * 0.021);
            int L3 = (int)Math.Round(C21 * 0.013);
            int L4 = (int)Math.Round(C21 * 0.008);
            int L5 = (int)Math.Round(C21 * 0.005); //10
            int L6 = (int)Math.Round(L5 * 0.618);  // 6
            int L7 = (int)Math.Round(L6 * 0.618);  // 4
            int L8 = (int)Math.Round(L7 * 0.618);  // 2
            int L9 = (int)Math.Round(L8 * 0.618);  // 1
            int L10 = (int)Math.Round(L9 * 0.618); // 1

            mapPeriods["C1"] = C1;
            mapPeriods["C2"] = C2;
            mapPeriods["C3"] = C3;
            mapPeriods["C5"] = C5;
            mapPeriods["C8"] = C8;
            mapPeriods["C13"] = C13;
            mapPeriods["C21"] = C21;
            mapPeriods["C34"] = C34;
            mapPeriods["C55"] = C55;
            mapPeriods["C89"] = C89;
            mapPeriods["C144"] = C144;
            //mapPeriods["C233"] = C233;
            //mapPeriods["C377"] = C377;
            //mapPeriods["C610"] = C610;
            mapPeriods["L1"] = L1;
            mapPeriods["L2"] = L2;
            mapPeriods["L3"] = L3;
            mapPeriods["L4"] = L4;
            mapPeriods["L5"] = L5;
            mapPeriods["L6"] = L6;
            mapPeriods["L7"] = L7;
            mapPeriods["L8"] = L8;
            mapPeriods["L9"] = L9;
            mapPeriods["L10"] = L10;

            var orderedPeriods = new List<KeyValuePair<string, int>>();
            foreach (var key in mapPeriods.Keys)
            {
                orderedPeriods.Add(new KeyValuePair<string, int>(key, mapPeriods[key]));
            }
            orderedPeriods.Sort((a, b) => a.Value.CompareTo(b.Value));

            lengthSeq = new List<string>();
            orderedPeriods.ForEach(lenArr => lengthSeq.Add(lenArr.Key));
        }

        public string GetFirst()
        {
            return lengthSeq[0];
        }

        public string GetLast()
        {
            return lengthSeq[lengthSeq.Count - 1];
        }

        public string? GetRelative(string len, int rel)
        {
            var idx = lengthSeq.IndexOf(len);
            if (idx == -1)
            {
                return null;
            }
            var newIdx = idx + rel;
            if (newIdx < 0 || newIdx >= lengthSeq.Count)
            {
                return null;
            }
            return lengthSeq[newIdx];
        }

        public int GetPeriod(string len)
        {
            // check if len is in mapPeriods and return the value.. if does not exist return 0
            if (!mapPeriods.ContainsKey(len))
            {
                return 0;
            }
            return mapPeriods[len];
        }
    }
}





