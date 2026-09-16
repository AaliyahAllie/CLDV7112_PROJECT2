using System.Net;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    /// <summary>
    /// Azure Function 4: Archives invoices, vendor contracts, and logistics documents into Azure File Share ('contracts').
    /// Runs silently whenever a customer checks out or an admin uploads a supplier agreement.
    /// </summary>
    public class UploadAzureFileFunction
    {
        private readonly ILogger _logger;

        public UploadAzureFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadAzureFileFunction>();
        }

        // HTTP trigger function listening at /api/UploadAzureFile
        [Function("UploadAzureFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadAzureFile")] HttpRequestData req)
        {
            _logger.LogInformation("Processing invoice / vendor agreement archiving for Azure File Share...");

            // Read contract or invoice document payload
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                // Parse document JSON payload
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                string shareName = root.TryGetProperty("ShareName", out var sProp) ? sProp.GetString() ?? "contracts" : "contracts";
                string fileName = root.TryGetProperty("FileName", out var fProp) ? fProp.GetString() ?? $"contract_{Guid.NewGuid():N}.txt" : $"contract_{Guid.NewGuid():N}.txt";
                string content = root.TryGetProperty("Content", out var cProp) ? cProp.GetString() ?? requestBody : requestBody;

                byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(content);

                // Fetch Azure Storage connection string
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                // Connect to Azure File Share 'contracts' and root directory
                var shareClient = new ShareClient(connString, shareName);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                // Create file and upload document text range
                await fileClient.CreateAsync(dataBytes.Length);
                using (var ms = new MemoryStream(dataBytes))
                {
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, dataBytes.Length), ms);
                }

                // Return direct cloud URI of saved file
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully archived document '{fileName}' in Azure File Share '{shareName}' via serverless function.",
                    ShareName = shareName,
                    FileName = fileName,
                    FileUri = fileClient.Uri.ToString()
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload document to Azure File Share.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
