using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace NII.Controllers
{
    [NII.Security.ApiKeyAuthenticationFilter]
    [RoutePrefix("api/retrieval")]
    public class ClientOutboundController : ApiController
    {
        private readonly NII.Security.Contracts.ISecureHttpClientFactory _clientFactory;
        private readonly NII.Security.Configuration.SecuritySettings _settings;

        public ClientOutboundController(NII.Security.Contracts.ISecureHttpClientFactory clientFactory, Microsoft.Extensions.Options.IOptions<NII.Security.Configuration.SecuritySettings> options)
        {
            _clientFactory = clientFactory;
            _settings = options.Value;
        }

        [HttpGet]
        [Route("call-azure")]
        public async Task<IHttpActionResult> CallAzureService()
        {
            var client = _clientFactory.GetClient();
            var response = await client.GetAsync(_settings.AzureTargetUrl);
            response.EnsureSuccessStatusCode();
            return Ok(await response.Content.ReadAsStringAsync());
        }
    }
}