using System;

namespace FIGCommon.Exceptions
{
    public class SqlUpdateException : Exception
    {
        public SqlUpdateException(string msg) : base(msg) { }
    }
}
