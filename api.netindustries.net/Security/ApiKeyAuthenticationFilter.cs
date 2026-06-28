using NII.Security.Contracts;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Filters;

namespace NII.Security
{
    public class ApiKeyAuthenticationFilter : Attribute, IAuthenticationFilter
    {
        private const string apiKeyName = "X-API-KEY";

        public bool AllowMultiple => false;

        public async Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {
            var request = context.Request;
            if (!request.Headers.Contains(apiKeyName))
            {
                context.ErrorResult = new AuthenticationFailureResult("Missing API Key header.", request);
                return;
            }

            string providedKey = request.Headers.GetValues(apiKeyName).Single();
            var identityRepo = (IClientIdentityRepository)request.GetDependencyScope().GetService(typeof(IClientIdentityRepository));

            try
            {
                string providedKeyHash = ComputeSha256Hash(providedKey);
                var identityRecord = await identityRepo.VerifyIdentityByHashAsync(providedKeyHash);

                if (identityRecord != null && identityRecord.Item3)
                {
                    context.Principal = new GenericPrincipal(new GenericIdentity(identityRecord.Item1), new[] { "User" });
                }
                else
                {
                    context.ErrorResult = new AuthenticationFailureResult("Unauthorized API Key confirmation.", request);
                }
            }
            catch
            {
                context.ErrorResult = new AuthenticationFailureResult("Security runtime isolation error.", request);
            }
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken) => Task.FromResult(0);

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
            }
        }
    }

    public class AuthenticationFailureResult : IHttpActionResult
    {
        private readonly string _reason;
        private readonly HttpRequestMessage _request;
        public AuthenticationFailureResult(string reason, HttpRequestMessage request) { _reason = reason; _request = request; }
        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized) { RequestMessage = _request, Content = new StringContent(_reason) });
    }
}