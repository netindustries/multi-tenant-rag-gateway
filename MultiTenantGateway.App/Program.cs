using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using System.Security.Cryptography.X509Certificates;
using Azure;
using Microsoft.Graph;
using MultiTenantGateway.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.Configure<GatewayOptions>(context.Configuration.GetSection(GatewayOptions.SectionName));

        var clientTrustStore = new Dictionary<string, (X509Certificate2 Certificate, string ClientId)>();
        var options = context.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>()
                      ?? new GatewayOptions();

        var defaultCredential = new DefaultAzureCredential();
        var kvClient = new CertificateClient(new Uri(options.AzureKeyVaultUri), defaultCredential);
        var graphClient = new GraphServiceClient(defaultCredential);

        try
        {
            Pageable<CertificateProperties> certificateList = kvClient.GetPropertiesOfCertificates();
            foreach (var certMetadata in certificateList)
            {
                if (!certMetadata.Enabled ?? false || certMetadata.ExpiresOn < DateTimeOffset.UtcNow) continue;

                try
                {
                    X509Certificate2 fullCertificate = kvClient.DownloadCertificate(certMetadata.Name);
                    string clientNameKey = fullCertificate.GetNameInfo(X509NameType.SimpleName, false);

                    // 1. EXTRACT THE IMMUTABLE CRYPTOGRAPHIC THUMBPRINT
                    // Microsoft Graph expects the hex string to be perfectly matched (often uppercase)
                    string certThumbprint = fullCertificate.Thumbprint.ToUpperInvariant();

                    // 2. CRYPTOGRAPHIC DIRECT DIRECTORY LOOKUP
                    // Filters the applications directory based on the nested keyCredentials custom identifier (Thumbprint)
                    var appSearchTask = graphClient.Applications
                        .GetAsync(config =>
                        {
                            // Enforces an advanced OData filter looking directly at the bound cert keys
                            config.QueryParameters.Filter = $"keyCredentials/any(c:c/customKeyIdentifier eq {certThumbprint})";
                            config.QueryParameters.Select = new[] { "appId" };

                            // Mandatory headers required by Entra ID when querying nested collection attributes
                            config.Headers.Add("ConsistencyLevel", "eventual");
                        });

                    appSearchTask.Wait();
                    var appsCollection = appSearchTask.Result;
                    var matchingApp = appsCollection?.Value?.FirstOrDefault();

                    if (matchingApp != null && !string.IsNullOrEmpty(matchingApp.AppId))
                    {
                        string discoveredClientId = matchingApp.AppId;

                        // Map: "net-industries" -> (Cert, GUID)
                        clientTrustStore.Add(clientNameKey, (fullCertificate, discoveredClientId));
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No Entra application holds a binding matching thumbprint '{certThumbprint}'.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error mapping dynamic cryptographic lookup for '{certMetadata.Name}': {ex.Message}");
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"Trust store bootstrap failure: {ex.Message}"); }

        services.AddSingleton(clientTrustStore);
        services.AddSingleton<SecurityEngine>();
        services.AddHttpClient();
    })
    .Build();

await host.RunAsync();
