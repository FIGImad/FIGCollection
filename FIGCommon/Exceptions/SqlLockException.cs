namespace FIGCommon.Exceptions
{
    public class SqlLockException : Exception
    {
        public SqlLockException(string msg) : base(msg) { }
    }
}

