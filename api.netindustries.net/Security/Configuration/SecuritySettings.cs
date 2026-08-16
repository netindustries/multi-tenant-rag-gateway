namespace NII.Security.Configuration
{
    public class SecuritySettings
    {
        public string SecureConn { get; set; }
        public int HttpClientTimeoutSeconds { get; set; }
        public string AzureTargetUrl { get; set; }
    }
}
