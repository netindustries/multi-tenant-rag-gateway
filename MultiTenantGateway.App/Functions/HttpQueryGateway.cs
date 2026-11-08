using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using Azure.Core;
using Azure.Search.Documents;
using Azure.AI.TextAnalytics;
using MultiTenantGateway.Core;
using Azure.Search.Documents.Models;
using Azure;

public class HttpQueryGateway
{
    private readonly SecurityEngine _security;
    private readonly GatewayOptions _options;

    public HttpQueryGateway(SecurityEngine security, IOptions<GatewayOptions> options)
    {
        _security = security;
        _options = options.Value;
    }

    [Function("SecureAgentEndpoint")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rag/agent")] HttpRequestData req)
    {
        var authResult = await _security.ValidateAndSwapAsync(req);
        if (!authResult.IsValid)
        {
            var reject = req.CreateResponse(HttpStatusCode.Forbidden);
            await reject.WriteStringAsync("Identity Denied."); 
            return reject;
        }

        string userPrompt = await new StreamReader(req.Body).ReadToEndAsync();
        var staticCredential = new StaticTokenCredential(authResult.Token);

        var textAnalyticsClient = new TextAnalyticsClient(new Uri(_options.AzureLanguageEndpoint), staticCredential);
        Response<KeyPhraseCollection> phrases = await textAnalyticsClient.ExtractKeyPhrasesAsync(userPrompt);
        string keywordQuery = phrases.Value.Any() ? phrases.Value.First() : userPrompt;

        var searchClient = new SearchClient(new Uri(_options.AzureSearchEndpoint), "knowledge-base-index", staticCredential);
        var searchOptions = new SearchOptions
        {
            Filter = $"TenantId eq '{authResult.TenantName}'", 
            Size = 2
        };

        var searchResults = await searchClient.SearchAsync<SearchDocument>(keywordQuery, searchOptions);

        var okResponse = req.CreateResponse(HttpStatusCode.OK);
        await okResponse.WriteAsJsonAsync(new { tenant = authResult.TenantName, status = "Verified", agentMvpHit = true });
        return okResponse;
    }
}