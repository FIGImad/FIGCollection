using Newtonsoft.Json.Linq;

namespace FIGCommon.Models
{
    public class PriceProviderDBParams
    {
        public string connStr = "";
        public string remoteTable = "";
        public string remoteInterval = "";
        public bool useBroker = false;
        public List<string> tickers = new List<string>();
        public List<string> targetIntervals = new List<string>();
        public PriceProviderDBParams() { }

        public static List<string> ParseStrings(object? _obj)
        {
            var strList = new List<string>();
            if (_obj == null)
            {
                return strList;
            }
            Type valueType = _obj.GetType();
            if (valueType.Name == "JArray")
            {
                var jStrings = _obj as JArray;
                if (jStrings != null)
                {
                    foreach (var item in jStrings)
                    {
                        strList.Add(item.ToString());
                    }
                    return strList;
                }
            }
            return strList;
        }

    }

}
