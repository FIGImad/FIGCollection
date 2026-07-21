
namespace FIGCommon.Exceptions
{
    public class TAException : Exception
    {
        public TAException(string msg) : base(msg) { }
        public int errorCode = 0;
    }
}
