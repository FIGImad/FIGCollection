using Newtonsoft.Json;

namespace FIGCommon.Models.FIGProviderAPI
{
    public class CancelStatusDto
    {
        public string OrderId { get; set; } = string.Empty;   // Order Table Id
        public string BrokerRef { get; set; } = string.Empty;  // PermId or broker trade id
        public int StatusCode { get; set; } = OrderStatusCodes.NEW;
        public long StatusTime { get; set; } = 0;

        public CancelStatusDto()
        {
            OrderId = string.Empty;
            BrokerRef = string.Empty;
            StatusCode = OrderStatusCodes.NEW;
            StatusTime = 0;
        }

        public CancelStatusDto(CancelStatusDto other)
        {
            OrderId = other.OrderId;
            BrokerRef = other.BrokerRef;
            StatusCode = other.StatusCode;
            StatusTime = other.StatusTime;
        }
    }
}
