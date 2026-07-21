using System;

namespace FIGCommon.Exceptions
{
    public class SqlProcException : Exception
    {
        public SqlProcException(string msg) : base(msg) { }
    }
}
