
namespace FIG.Studies
{
    public class BaseCycles
    {
        public int cycleMult;
        public int minCycle = 1;
        public int maxCycle = 100;
        public List<CycleInfo> cycleList = new List<CycleInfo>();
        public Dictionary<int, CycleInfo> cycleMap = new Dictionary<int, CycleInfo>();

        public BaseCycles(int minCycle, int maxCycle, int multiplier) 
        {
            this.cycleMult = multiplier;
            this.cycleMap = new Dictionary<int, CycleInfo>();
            int ndx = 0;
            for (int cycleNum = minCycle; cycleNum <= maxCycle; cycleNum++)
            {
                int period = cycleNum * cycleMult;
                var studyDef = new CycleInfo
                {
                    ndx = ndx,
                    cycleNum = cycleNum,
                    period = period,
                };
                this.cycleMap.Add(cycleNum, studyDef);   // add it to the map to retrieve faster
                this.cycleList.Add(studyDef);

                ndx++;
            }
        }

        // getters and setters
        public int CyclesLength { get => cycleList.Count; }
        public int MaxCycle { get => maxCycle; set => maxCycle = value; }
        public int CycleMult { get => cycleMult; set => cycleMult = value; }

        public BaseCycles Clone()
        {
            return Clone(this);
        }

        public BaseCycles Clone(BaseCycles obj)
        {
            return new BaseCycles(obj.minCycle, obj.maxCycle, obj.cycleMult);
        }

        public CycleInfo? GetStudyDefFromCycle(int cycleNum)
        {
            if (cycleNum == 0 || cycleMap.Count == 0) return null;
            return cycleMap[cycleNum];
        }
        public CycleInfo? GetStudyDefFromNdx(int ndx)
        {
            if (ndx < 0 || ndx >= cycleList.Count) return null;
            return cycleList[ndx];
        }

    }
}



