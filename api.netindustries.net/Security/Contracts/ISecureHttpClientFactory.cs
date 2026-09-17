using System.Net.Http;

namespace NII.Security.Contracts
{
    public interface ISecureHttpClientFactory
    {
        HttpClient GetClient();
        void FlushCache();
    }
}
