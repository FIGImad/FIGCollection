using FIGCommon.Utilities;

namespace FIGCommon.Controller
{
    public class ErrorResponseObj
    {
        public int ErrorCode { get; set; } = ErrorCodes.UnSpecified;
        public string ServiceCode { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
