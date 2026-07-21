namespace FIGCommon.Exceptions
{
    public class SqlUpsertException : Exception
    {
        public SqlUpsertException(string msg) : base(msg) { }
    }
}
