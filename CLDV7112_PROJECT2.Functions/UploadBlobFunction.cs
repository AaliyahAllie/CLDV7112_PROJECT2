using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    public class UploadBlobFunction
    {
        private readonly ILogger _logger;

        public UploadBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadBlobFunction>();
        }

        [Function("UploadBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadBlob")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Azure Blob Storage upload request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string containerName = root.TryGetProperty("ContainerName", out var cProp) ? cProp.GetString() ?? "product-images" : "product-images";
                string blobName = root.TryGetProperty("BlobName", out var bProp) ? bProp.GetString() ?? $"blob_{Guid.NewGuid():N}.txt" : $"blob_{Guid.NewGuid():N}.txt";
                string contentBase64 = root.TryGetProperty("ContentBase64", out var contentProp) ? contentProp.GetString() ?? "" : "";
                string contentType = root.TryGetProperty("ContentType", out var typeProp) ? typeProp.GetString() ?? "text/plain" : "text/plain";

                byte[] dataBytes = !string.IsNullOrEmpty(contentBase64) 
                    ? Convert.FromBase64String(contentBase64)
                    : System.Text.Encoding.UTF8.GetBytes(requestBody);

                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                var blobServiceClient = new BlobServiceClient(connString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(blobName);
                using var ms = new MemoryStream(dataBytes);
                await blobClient.UploadAsync(ms, overwrite: true);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully uploaded blob '{blobName}' to Azure Blob container '{containerName}' via Azure Function.",
                    Container = containerName,
                    BlobName = blobName,
                    BlobUrl = blobClient.Uri.ToString()
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Azure Blob Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
