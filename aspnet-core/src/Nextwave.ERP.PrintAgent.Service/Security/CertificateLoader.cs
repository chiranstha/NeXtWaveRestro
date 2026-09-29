using System.Security.Cryptography.X509Certificates;

namespace Nextwave.ERP.PrintAgent.Service.Security;

public static class CertificateLoader
{
    public const string SubjectName = "Nextwave ERP Print Agent";

    public static X509Certificate2 Load()
    {
        return FindCertificate(StoreLocation.LocalMachine)
               ?? FindCertificate(StoreLocation.CurrentUser)
               ?? throw new InvalidOperationException("The Nextwave ERP Print Agent HTTPS certificate is not installed.");
    }

    private static X509Certificate2? FindCertificate(StoreLocation location)
    {
        using var store = new X509Store(StoreName.My, location);
        store.Open(OpenFlags.ReadOnly);
        return store.Certificates
            .Find(X509FindType.FindBySubjectName, SubjectName, validOnly: true)
            .OfType<X509Certificate2>()
            .OrderByDescending(certificate => certificate.NotAfter)
            .FirstOrDefault(certificate => certificate.HasPrivateKey);
    }
}
