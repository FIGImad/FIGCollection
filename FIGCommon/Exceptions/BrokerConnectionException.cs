namespace FIGCommon.Exceptions
{

    public class BrokerConnectionException : Exception
    {
        public BrokerConnectionException(string errorMsg) : base(errorMsg) { }
    }
}
