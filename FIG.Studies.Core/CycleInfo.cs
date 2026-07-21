namespace FIG.Studies
{
    public class CycleInfo
    {
        public int ndx;
        public int cycleNum;
        public int period;

        public CycleInfo Clone()
        {
            return Clone(this);
        }

        public CycleInfo Clone(CycleInfo obj)
        {
            CycleInfo newDef = new CycleInfo();
            newDef.ndx = obj.ndx;
            newDef.cycleNum = obj.cycleNum;
            newDef.period = obj.period;
            return newDef;
        }
    }
}



