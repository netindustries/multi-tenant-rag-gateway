namespace MultiTenantGateway.Core
{
    public class GatewayOptions
    {
        public const string SectionName = "GatewayConfiguration";

        public string AzureKeyVaultUri { get; set; } = string.Empty;
        public string AzureTenantId { get; set; } = string.Empty;
        public string AzureSearchEndpoint { get; set; } = string.Empty;
        public string AzureLanguageEndpoint { get; set; } = string.Empty;
    }
}