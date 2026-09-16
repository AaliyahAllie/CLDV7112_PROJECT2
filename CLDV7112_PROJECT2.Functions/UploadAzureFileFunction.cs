using System.Net;
using System.Text.Json;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    public class UploadAzureFileFunction
    {
        private readonly ILogger _logger;

        public UploadAzureFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadAzureFileFunction>();
        }

        [Function("UploadAzureFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "UploadAzureFile")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Azure File Share upload request.");

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

                string shareName = root.TryGetProperty("ShareName", out var sProp) ? sProp.GetString() ?? "contracts" : "contracts";
                string fileName = root.TryGetProperty("FileName", out var fProp) ? fProp.GetString() ?? $"contract_{Guid.NewGuid():N}.txt" : $"contract_{Guid.NewGuid():N}.txt";
                string content = root.TryGetProperty("Content", out var cProp) ? cProp.GetString() ?? requestBody : requestBody;

                byte[] dataBytes = System.Text.Encoding.UTF8.GetBytes(content);

                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                var shareClient = new ShareClient(connString, shareName);
                await shareClient.CreateIfNotExistsAsync();

                var directoryClient = shareClient.GetRootDirectoryClient();
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.CreateAsync(dataBytes.Length);
                using (var ms = new MemoryStream(dataBytes))
                {
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, dataBytes.Length), ms);
                }

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully uploaded file '{fileName}' to Azure File Share '{shareName}' via Azure Function.",
                    ShareName = shareName,
                    FileName = fileName,
                    FileUri = fileClient.Uri.ToString()
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading to Azure Files.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
