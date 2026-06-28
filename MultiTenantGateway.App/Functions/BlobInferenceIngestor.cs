using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Search.Documents;
using Azure.AI.TextAnalytics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantGateway.Core;

public class BlobInferenceIngestor
{
    private readonly ILogger _logger;
    private readonly GatewayOptions _options;

    public BlobInferenceIngestor(ILoggerFactory loggerFactory, IOptions<GatewayOptions> options)
    {
        _logger = loggerFactory.CreateLogger<BlobInferenceIngestor>();
        _options = options.Value;
    }

    [Function("MultiTenantBlobInferenceIngestor")]
    public async Task Run([BlobTrigger("document-silo-{clientName}/{name}", Connection = "ClientStorageConnection")] BlobClient blobClient, string clientName, string name)
    {
        try
        {
            var downloadResponse = await blobClient.DownloadContentAsync();
            string textChunk = downloadResponse.Value.Content.ToString();

            var textAnalyticsClient = new TextAnalyticsClient(new Uri(_options.AzureLanguageEndpoint), new DefaultAzureCredential());
            var phrasesResponse = await textAnalyticsClient.ExtractKeyPhrasesAsync(textChunk);
            string searchableMetadataString = string.Join(" ", phrasesResponse.Value);

            var searchClient = new SearchClient(new Uri(_options.AzureSearchEndpoint), "knowledge-base-index", new DefaultAzureCredential());
            var searchDoc = new Dictionary<string, object> {
                { "id", Guid.NewGuid().ToString() }, 
                { "content", textChunk },
                { "searchable_metadata", searchableMetadataString }, 
                { "TenantId", clientName }
            };

            await searchClient.UploadDocumentsAsync(new[] { searchDoc });
        }
        catch (Exception ex) 
        { 
            _logger.LogError($"Ingestion processing error: {ex.Message}"); 
            throw; 
        }
    }
}