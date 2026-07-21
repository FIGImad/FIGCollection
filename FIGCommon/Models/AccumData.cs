using Newtonsoft.Json;

namespace FIGCommon.Models
{
    public class AccumData
    {
        public int DealQty { get; set; } = 0;
        public decimal DealVal { get; set; } = 0;
        public decimal DealCommission { get; set; } = 0;
        public decimal DealClosePL { get; set; } = 0;
        public decimal DealOpenPL { get; set; } = 0;
        protected decimal multiplier = 1M;
        protected int prevDealQty = 0;

        public AccumData() { }
        public AccumData(decimal multiplier)
        {
            this.multiplier = multiplier;
        }
        public AccumData(AccumData data)
        {
            Clone(data);
        }

        public void Clone(AccumData src)
        {
            this.DealQty = src.DealQty;
            this.DealVal = src.DealVal;
            this.DealCommission = src.DealCommission;
            this.DealClosePL = src.DealClosePL;
            this.DealOpenPL = src.DealOpenPL;
            this.multiplier = src.multiplier;
        }

        public void InitCumulativeData(string paramStr)
        {
            DealQty = 0;
            DealVal = 0;
            DealCommission = 0;
            DealClosePL = 0;
            DealOpenPL = 0;
            prevDealQty = 0;

            try
            {
                var data = JsonConvert.DeserializeObject<AccumData>(paramStr);
                if (data != null)
                {
                    data.multiplier = this.multiplier;
                    Clone(data);
                    prevDealQty = data.DealQty;
                }
            }
            catch { }
        }

        public void UpdateAccumValues(int qty, decimal tradeVal, decimal commission, decimal closePrice)
        {
            DealQty += qty;
            DealVal += tradeVal;
            DealCommission += commission;
            DealClosePL = DealQty == 0 && prevDealQty != 0 ? DealVal : DealClosePL;
            DealOpenPL = DealQty == 0 ? 0M : (closePrice * multiplier * DealQty) + (DealVal - DealClosePL);
            prevDealQty = DealQty;
        }


        // I need to override ToString function
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.None);
        }

    }
}