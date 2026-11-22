using Microsoft.Extensions.Options;
using NII.Security.Configuration;
using NII.Security.Contracts;
using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NII.Security.Services
{
    public class SecureHttpClientFactory : ISecureHttpClientFactory
    {
        private readonly IInfrastructureSecretRepository _infraRepo;
        private readonly SecuritySettings _settings;
        private readonly object _lockObject = new object();
        private HttpClient _cachedClient;

        public SecureHttpClientFactory(IInfrastructureSecretRepository infraRepo, IOptions<SecuritySettings> options)
        {
            _infraRepo = infraRepo ?? throw new ArgumentNullException(nameof(infraRepo));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public HttpClient GetClient()
        {
            if (_cachedClient == null)
            {
                lock (_lockObject)
                {
                    if (_cachedClient == null)
                    {
                        _cachedClient = BuildAuthenticatedClient();
                    }
                }
            }
            return _cachedClient;
        }

        public void FlushCache()
        {
            lock (_lockObject)
            {
                if (_cachedClient != null)
                {
                    try { _cachedClient.Dispose(); }
                    catch { }
                    finally { _cachedClient = null; }
                }
            }
        }

        private HttpClient BuildAuthenticatedClient()
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072 | (SecurityProtocolType)12288;
            X509Certificate2 cert = FetchCertificateFromDatabase();

            var handler = new HttpClientHandler();
            handler.ClientCertificates.Add(cert);

            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(_settings.HttpClientTimeoutSeconds);
            return client;
        }

        private X509Certificate2 FetchCertificateFromDatabase()
        {
            var certData = Task.Run(() => _infraRepo.FetchSecretAsync(SecretKeyNames.ClientCertificateFile)).Result;
            byte[] certBytes = CryptographyEngine.Decrypt(certData.Item1, certData.Item2);

            var passData = Task.Run(() => _infraRepo.FetchSecretAsync(SecretKeyNames.ClientCertificatePassword)).Result;
            byte[] decryptedPassBytes = CryptographyEngine.Decrypt(passData.Item1, passData.Item2);
            string certPassword = Encoding.UTF8.GetString(decryptedPassBytes);

            return new X509Certificate2(certBytes, certPassword, X509KeyStorageFlags.DefaultKeySet);
        }
    }
}
