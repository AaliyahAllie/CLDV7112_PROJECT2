using System.Net;
using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    public class StoreTableInfoFunction
    {
        private readonly ILogger _logger;

        public StoreTableInfoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StoreTableInfoFunction>();
        }

        [Function("StoreTableInfo")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "StoreTableInfo")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Azure Table Storage insert request.");

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

                string tableName = root.TryGetProperty("TableName", out var tProp) ? tProp.GetString() ?? "Customers" : "Customers";
                string partitionKey = root.TryGetProperty("PartitionKey", out var pProp) ? pProp.GetString() ?? "General" : "General";
                string rowKey = root.TryGetProperty("RowKey", out var rProp) ? rProp.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString();

                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                var serviceClient = new TableServiceClient(connString);
                var tableClient = serviceClient.GetTableClient(tableName);
                await tableClient.CreateIfNotExistsAsync();

                var entity = new TableEntity(partitionKey, rowKey);

                foreach (var prop in root.EnumerateObject())
                {
                    if (prop.Name != "TableName" && prop.Name != "PartitionKey" && prop.Name != "RowKey")
                    {
                        entity[prop.Name] = prop.Value.ToString();
                    }
                }

                entity["Timestamp"] = DateTimeOffset.UtcNow;
                await tableClient.UpsertEntityAsync(entity);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = $"Successfully stored entity in Azure Table '{tableName}' via Azure Function.",
                    TableName = tableName,
                    PartitionKey = partitionKey,
                    RowKey = rowKey
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing information in Azure Table.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
