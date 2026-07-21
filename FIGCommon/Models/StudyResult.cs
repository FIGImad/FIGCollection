using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;


namespace FIGCommon.Models
{

    public class StudyResult
    {

        public int Id { get; set; }
        public List<PriceDataRS> PriceData { get; set; }
        public ArrayList Data { get; set; }

        public StudyResult(int id, List<PriceDataRS> priceData)
        {
            Id = id;
            PriceData = priceData;
            Data = new ArrayList();
        }

    }
}
