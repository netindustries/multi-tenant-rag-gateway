using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Azure.Core;
using Azure.Identity;
using MultiTenantGateway.Core;

public class SecurityEngine
{
    private readonly Dictionary<string, (X509Certificate2 Certificate, string ClientId)> _trustStore;
    private readonly GatewayOptions _options;

    public SecurityEngine(Dictionary<string, (X509Certificate2 Certificate, string ClientId)> trustStore, IOptions<GatewayOptions> options)
    {
        _trustStore = trustStore;
        _options = options.Value;
    }

    public async Task<(bool IsValid, string TenantName, string Token)> ValidateAndSwapAsync(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("X-ARR-ClientCert", out var headerValues)) return (false, string.Empty, string.Empty);

        try
        {
            string rawHeaderValue = headerValues.First();
            byte[] clientCertBytes = Convert.FromBase64String(rawHeaderValue);

            // Compute SHA-1 thumbprint from raw certificate bytes (matches X509Certificate2.Thumbprint)
            string incomingThumbprint;
            using (var sha1 = SHA1.Create())
            {
                incomingThumbprint = BitConverter.ToString(sha1.ComputeHash(clientCertBytes)).Replace("-", string.Empty);
            }

            // Find matching client in trust store by thumbprint (avoid constructing X509Certificate2 from raw bytes)
            foreach (var kvp in _trustStore)
            {
                if (string.Equals(kvp.Value.Certificate.Thumbprint, incomingThumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    var clientName = kvp.Key;
                    var clientContext = kvp.Value;

                    var clientCertificateCredential = new ClientCertificateCredential(
                        _options.AzureTenantId, clientContext.ClientId, clientContext.Certificate
                    );

                    var tokenRequestContext = new TokenRequestContext(new[] { "https://azure.com", "https://windows.net" });
                    AccessToken entraToken = await clientCertificateCredential.GetTokenAsync(tokenRequestContext);

                    return (true, clientName, entraToken.Token);
                }
            }
        }
        catch { /* Context Dropped */ }
        return (false, string.Empty, string.Empty);
    }
}