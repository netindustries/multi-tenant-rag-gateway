using System.Configuration;
using Microsoft.Extensions.Options;

namespace NII.Security.Configuration
{
    public static class AppOptionsFactory
    {
        public static IOptions<SecuritySettings> CreateSecuritySettings()
        {
            if (!int.TryParse(ConfigurationManager.AppSettings[SecretKeyNames.WebConfigHttpClientTimeoutSeconds], out int timeout))
            {
                timeout = 30;
            }

            return Options.Create(new SecuritySettings
            {
                SecureConn = ConfigurationManager.AppSettings[SecretKeyNames.WebConfigSecureConn],
                HttpClientTimeoutSeconds = timeout,
                AzureTargetUrl = ConfigurationManager.AppSettings[SecretKeyNames.WebConfigAzureTargetUrl]
            });
        }
    }
}
