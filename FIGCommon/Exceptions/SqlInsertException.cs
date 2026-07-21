using System;

namespace FIGCommon.Exceptions
{
    public class SqlInsertException : Exception
    {
        public SqlInsertException(string msg) : base(msg) { }
    }
}
