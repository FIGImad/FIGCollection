using System.IO.Compression;
using System.Text;

namespace ATS.Utilities
{
    public class FileUtils
    {
        public static string GetTempFileName(string directory, string prefix = "", string extension = "")
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = null;
            int tries = 0;

            do
            {                
                fileName = Path.Combine(directory, $"{prefix}{Path.GetRandomFileName()}.{extension}");
                try
                {
                    using (var fileStram = System.IO.File.Open(fileName, FileMode.CreateNew))
                    {
                        break;
                    }
                }
                catch (IOException)
                {
                    tries++;
                }
            } while (tries < 10);

            return fileName;
        }

        public static bool SafeDeleteFile(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    System.IO.File.Delete(path);
                    return true;
                }
                catch (Exception )
                {
                }
            }
            return false;
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }
}
