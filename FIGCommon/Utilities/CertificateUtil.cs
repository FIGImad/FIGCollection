using System.Security.Cryptography.X509Certificates;

namespace FIGCommon.Utilities
{
    public class CertificateUtil
    {
        public static X509Certificate2? GetCertificateFromStore(string thumbprint, StoreName storeName = StoreName.My)
        {
            using (var store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    validOnly: false);

                return certs.Count > 0 ? certs[0] : null;
            }
        }
    }

}
