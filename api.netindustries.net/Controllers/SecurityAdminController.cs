using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using NII.Models;
using NII.Security;
using NII.Security.Services;
using NII.Security.Contracts;

namespace NII.Controllers
{
    [RoutePrefix("api/admin")]
    [NII.Security.ApiKeyAuthenticationFilter]
    public class SecurityAdminController : ApiController
    {
        private readonly IInfrastructureSecretRepository _infraRepo;
        private readonly IClientIdentityRepository _identityRepo;
        private readonly ISecureHttpClientFactory _clientFactory;

        // Pure Constructor Dependency Injection managed by MicrosoftDependencyResolver
        public SecurityAdminController(
            IInfrastructureSecretRepository infraRepo,
            IClientIdentityRepository identityRepo,
            ISecureHttpClientFactory clientFactory)
        {
            _infraRepo = infraRepo ?? throw new ArgumentNullException(nameof(infraRepo));
            _identityRepo = identityRepo ?? throw new ArgumentNullException(nameof(identityRepo));
            _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        }

        [HttpPost]
        [Route("register-identity")]
        public async Task<IHttpActionResult> RegisterNewClientIdentity([FromBody] ClientRegistrationRequest request)
        {
            // 1. Parameter Validation Guards
            if (request == null) return BadRequest("Payload body cannot be null.");
            if (string.IsNullOrWhiteSpace(request.ClientName)) return BadRequest("ClientName is required.");
            if (string.IsNullOrWhiteSpace(request.EmailAddress) || !request.EmailAddress.Contains("@")) return BadRequest("Valid EmailAddress is required.");
            if (string.IsNullOrWhiteSpace(request.RawApiKeyToRegister) || request.RawApiKeyToRegister.Length < 16)
            {
                return ResponseMessage(Request.CreateErrorResponse(HttpStatusCode.BadRequest, "RawApiKeyToRegister is required and must be at least 16 characters."));
            }
            if (!Request.Content.IsMimeMultipartContent())
            {
                return Content(HttpStatusCode.UnsupportedMediaType, "Must be a multi-part form.");
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                byte[] rawCertFileBytes = null;
                string certPasswordText = null;

                foreach (var content in provider.Contents)
                {
                    string fieldName = content.Headers.ContentDisposition.Name.Trim('"');
                    if (fieldName == "certificateFile") rawCertFileBytes = await content.ReadAsByteArrayAsync();
                    else if (fieldName == "certificatePassword") certPasswordText = await content.ReadAsStringAsync();
                }

                if (rawCertFileBytes == null || string.IsNullOrWhiteSpace(certPasswordText))
                {
                    return BadRequest("Invalid certificate configuration parameters.");
                }

                // Encrypt assets safely in RAM
                var encryptedCert = CryptographyEngine.Encrypt(rawCertFileBytes);
                var encryptedPass = CryptographyEngine.Encrypt(Encoding.UTF8.GetBytes(certPasswordText));

                // Save via infrastructure command path
                await _infraRepo.SaveSecretAsync(SecretKeyNames.ClientCertificateFile, encryptedCert.Item1, encryptedCert.Item2);
                await _infraRepo.SaveSecretAsync(SecretKeyNames.ClientCertificatePassword, encryptedPass.Item1, encryptedPass.Item2);

                // Clear out active HTTP Client socket pools instantly
                _clientFactory.FlushCache();

                return Ok("Certificate components encrypted and saved successfully. Execution cache cleared.");
            }
            catch (Exception ex)
            {
                return InternalServerError(new Exception("An error occurred during certificate processing.", ex));
            }
        }
    }
}