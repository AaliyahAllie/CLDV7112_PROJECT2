using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    /// <summary>
    /// Azure Function 1: Saves customer profiles and store data into Azure Table Storage.
    /// Runs silently in the background when users sign up or admins update inventory.
    /// </summary>
    public class StoreTableInfoFunction
    {
        private readonly ILogger _logger;

        public StoreTableInfoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreTableInfoFunction>();
        }

        // HTTP trigger function listening at /api/StoreTableInfo
        [Function("StoreTableInfo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "StoreTableInfo")] HttpRequestData req)
        {
            _logger.LogInformation("Processing customer / inventory table storage request...");

            // Read the JSON payload sent from the web application
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                // Parse incoming JSON data
                using var doc = JsonDocument.Parse(requestBody);
                var root = doc.RootElement;

                // Extract table properties or fallback to sensible defaults
                string tableName = root.TryGetProperty("TableName", out var tProp) ? tProp.GetString() ?? "Customers" : "Customers";
                string partitionKey = root.TryGetProperty("PartitionKey", out var pProp) ? pProp.GetString() ?? "General" : "General";
                string rowKey = root.TryGetProperty("RowKey", out var rProp) ? rProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                // Retrieve Azure Storage connection string from cloud environment settings
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                // Connect to Azure Table Storage and ensure the table exists
                var serviceClient = new TableServiceClient(connString);
                var tableClient = serviceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

                // Build table entity dynamically from JSON data fields
                var entity = new TableEntity(partitionKey, rowKey);

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name != "TableName" && prop.Name != "PartitionKey" && prop.Name != "RowKey")
                    {
                        entity[prop.Name] = prop.Value.ToString();
                    }
                }

                entity["Timestamp"] = DateTimeOffset.UtcNow;
                
                // Save or update the record in Azure Tables
                await tableClient.UpsertEntityAsync(entity);

                // Return a friendly JSON success response back to the main app
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully saved record in Azure Table '{tableName}' via serverless function.",
                    TableName = tableName,
                    PartitionKey = partitionKey,
                    RowKey = rowKey
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store record in Azure Table Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
