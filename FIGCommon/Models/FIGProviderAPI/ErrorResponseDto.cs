namespace FIGCommon.Models.FIGProviderAPI
{
    public class ErrorResponseDto
    {
        public string BrokerOrderId { get; set; } = string.Empty;
        public int BrokerErrorCode { get; set; } = -1;

        public int ErrorCode { get; set; } = -1;  // FIG error code

        public string Message { get; set; }
        public ErrorResponseDto()
        {
            BrokerOrderId = string.Empty;
            BrokerErrorCode = -1;
            ErrorCode = -1;
            Message = string.Empty;
        }

        public ErrorResponseDto(ErrorResponseDto other)
        {
            BrokerOrderId = other.BrokerOrderId;
            BrokerErrorCode = other.BrokerErrorCode;
            ErrorCode = other.ErrorCode;
            Message = other.Message;
        }
    }
}
