namespace FIGCommon.Exceptions
{
    public class ProviderException : Exception
    {
        public int RawErrorCode { get; set; }
        public int ProviderErrorCode { get; set; }

        public ProviderException(int rawErrorCode, int providerErrorCode, string errorMsg) : base(errorMsg)
        {
            ProviderErrorCode = providerErrorCode;
            RawErrorCode = rawErrorCode;
        }
    }
}
