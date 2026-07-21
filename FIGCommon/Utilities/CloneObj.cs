using FIGCommon.Models;
using System.Security.Cryptography.Xml;

namespace FIGCommon.Utilities
{
    public class CloneObj
    {
        public static void Copy<T>(T src, T dst) where T : new()
        {
            var srcT = src?.GetType();
            var dstT = dst?.GetType();
            if (srcT == null || dstT == null)
            {
                return;
            }
            foreach (var f in srcT.GetFields())
            {
                var dstF = dstT.GetField(f.Name);
                if (dstF == null || dstF.IsLiteral)
                    continue;
                dstF.SetValue(dst, f.GetValue(src));
            }

            foreach (var f in srcT.GetProperties())
            {
                var dstF = dstT.GetProperty(f.Name);
                if (dstF == null)
                    continue;

                dstF.SetValue(dst, f.GetValue(src, null), null);
            }
        }
    }
}


