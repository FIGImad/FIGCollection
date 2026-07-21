namespace FIGCommon.Exceptions
{
    
    public class ResponseWaitException : Exception
    {
        public int ErrorCode { get; set; }

        public ResponseWaitException(int errorCode, string errorMsg) : base(errorMsg)
        {
            ErrorCode = errorCode;
        }
    }
}
