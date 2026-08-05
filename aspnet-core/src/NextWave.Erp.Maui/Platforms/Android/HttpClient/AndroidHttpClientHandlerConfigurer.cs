using System.Net.Security;

namespace NextWave.Erp.Maui.Core.Platforms.Android.HttpClient;

public class AndroidHttpClientHandlerConfigurer
{
    public void Configure(HttpClientHandler handler)
    {
#if DEBUG
        TrustLocalDeveloperCert(handler);
#endif
    }

    private void TrustLocalDeveloperCert(HttpClientHandler handler)
    {
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
        {
            if (cert != null && cert.Issuer.Equals("CN=localhost"))
            {
                return true;
            }

            return errors == SslPolicyErrors.None;
        };
    }
}