using FIGCommon.Utilities;

namespace FIGCommon.Exceptions
{
    public class AppErrorException : Exception
    {
        public int errorCode = 0;
        public string serviceCode = "";
        public AppErrorException(int errorCode, string serviceErrorCode, string? message = null) 
            : base(message == null ? ErrorCodes.ToText(errorCode) : message) 
        {
            this.errorCode = errorCode;
            this.serviceCode = serviceErrorCode;
        }
    }
}
