
namespace FIGCommon.Exceptions
{
    public class AppException : Exception
    {
        public AppException(string msg) : base(msg) { }
        public int errorCode = 0;
    }
}
