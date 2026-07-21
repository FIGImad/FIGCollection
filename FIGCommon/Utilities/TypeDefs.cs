namespace FIGCommon.Utilities
{

    public enum EnumTradeAction
    {
        CoverShort = -2,
        Short = -1,
        None = 0,
        Long = 1,
        CloseLong = 2
    }


    public enum EnumKeptPosCond
    {
        None = 0,
        Fixed = 1,
        PriceTarget = 2
    }

    //public static class OrderBookOrderType
    //{
    //    public const string None = "";
    //    public const string Market = "MARKET";
    //    public const string Limit = "LIMIT";
    //    public const string Stop = "STOP";
    //}
    //public static class OrderBookOrderAction
    //{
    //    public const string New = "NEW";
    //    public const string Update = "UPDATE";
    //    public const string Cancel = "CANCEL";
    //}
    //public static class OrderBookDealType
    //{
    //    public const string Open = "OPEN";
    //    public const string Close = "CLOSE";
    //}

    public enum EnumBarStatus
    {
        CurrentBar = 0,
        OneMinBar = 1,
        NewBar = 2
    }

    public static class EnumNameHelper
    {
        // function returns the name of EnumBarStatus enum in string
        public static string GetBarStatusName(EnumBarStatus barStatus)
        {
            return barStatus switch
            {
                EnumBarStatus.CurrentBar => "CurrentBar",
                EnumBarStatus.OneMinBar => "OneMinBar",
                EnumBarStatus.NewBar => "NewBar",
                _ => "Unknown",
            };
        }
    }

}