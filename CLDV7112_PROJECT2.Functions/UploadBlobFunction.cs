using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    /// <summary>
    /// Azure Function 2: Uploads product images and media files to Azure Blob Storage.
    /// Runs silently whenever admins or vendors upload product media assets.
    /// </summary>
    public class UploadBlobFunction
    {
        private readonly ILogger _logger;

        public UploadBlobFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadBlobFunction>();
        }

        // HTTP trigger function listening at /api/UploadBlob
        [Function("UploadBlob")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadBlob")] HttpRequestData req)
        {
            _logger.LogInformation("Processing product media upload request for Azure Blob Storage...");

            // Read raw JSON upload request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                // Parse payload details
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string containerName = root.TryGetProperty("ContainerName", out var cProp) ? cProp.GetString() ?? "product-images" : "product-images";
                string blobName = root.TryGetProperty("BlobName", out var bProp) ? bProp.GetString() ?? $"blob_{Guid.NewGuid():N}.txt" : $"blob_{Guid.NewGuid():N}.txt";
                string contentBase64 = root.TryGetProperty("ContentBase64", out var contentProp) ? contentProp.GetString() ?? "" : "";
                string contentType = root.TryGetProperty("ContentType", out var typeProp) ? typeProp.GetString() ?? "text/plain" : "text/plain";

                // Decode file bytes from base64 string
                byte[] dataBytes = !string.IsNullOrEmpty(contentBase64) 
                    ? Convert.FromBase64String(contentBase64)
                    : System.Text.Encoding.UTF8.GetBytes(requestBody);

                // Fetch Azure Storage connection string from cloud configuration
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                // Connect to Azure Blob Storage container and grant public read access for image display
                var blobServiceClient = new BlobServiceClient(connString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();
                await containerClient.SetAccessPolicyAsync(Azure.Storage.Blobs.Models.PublicAccessType.Blob);

                // Stream and upload the file to cloud storage
                var blobClient = containerClient.GetBlobClient(blobName);
                using var ms = new MemoryStream(dataBytes);
                await blobClient.UploadAsync(ms, overwrite: true);

                // Return direct HTTPS link to the uploaded media file
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully uploaded blob '{blobName}' to Azure Blob container '{containerName}' via serverless function.",
                    Container = containerName,
                    BlobName = blobName,
                    BlobUrl = blobClient.Uri.ToString()
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to Azure Blob Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
