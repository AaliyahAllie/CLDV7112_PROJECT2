using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    /// <summary>
    /// Azure Function 3: Manages transaction messages in Azure Queue Storage ('order-transactions').
    /// Pushes checkout transactions to the queue and processes them asynchronously.
    /// </summary>
    public class ProcessQueueTransactionFunction
    {
        private readonly ILogger _logger;

        public ProcessQueueTransactionFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessQueueTransactionFunction>();
        }

        // HTTP trigger function listening at /api/WriteQueueTransaction
        [Function("WriteQueueTransaction")]
        public async Task<HttpResponseData> WriteTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WriteQueueTransaction")] HttpRequestData req)
        {
            _logger.LogInformation("Pushing order checkout transaction to Azure Storage Queue...");

            // Read order transaction JSON body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                // Fetch storage connection string
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                // Connect to Azure Queue 'order-transactions'
                var queueClient = new QueueClient(connString, "order-transactions");
                await queueClient.CreateIfNotExistsAsync();

                // Encode message payload to base64 for reliable queue transmission
                string base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(requestBody));
                var receipt = await queueClient.SendMessageAsync(base64Message);

                // Return JSON confirmation with queue message ID
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Transaction message successfully published to Azure Queue 'order-transactions' via serverless function.",
                    MessageId = receipt.Value.MessageId,
                    PopReceipt = receipt.Value.PopReceipt,
                    ExpirationTime = receipt.Value.ExpirationTime
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write transaction message to Azure Queue Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }

        // HTTP trigger function listening at /api/ReadQueueTransaction
        [Function("ReadQueueTransaction")]
        public async Task<HttpResponseData> ReadTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadQueueTransaction")] HttpRequestData req)
        {
            _logger.LogInformation("Reading queued order transactions from Azure Storage Queue...");

            try
            {
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                // Connect to Azure Queue 'order-transactions'
                var queueClient = new QueueClient(connString, "order-transactions");
                await queueClient.CreateIfNotExistsAsync();

                // Fetch top 5 pending queue messages
                var response = await queueClient.ReceiveMessagesAsync(maxMessages: 5);
                var messagesList = new List<object>();

                foreach (var msg in response.Value)
                {
                    string decodedText;
                    try
                    {
                        var bytes = Convert.FromBase64String(msg.MessageText);
                        decodedText = System.Text.Encoding.UTF8.GetString(bytes);
                    }
                    catch
                    {
                        decodedText = msg.MessageText;
                    }

                    messagesList.Add(new
                    {
                        MessageId = msg.MessageId,
                        MessageText = decodedText,
                        InsertionTime = msg.InsertedOn,
                        ExpirationTime = msg.ExpiresOn
                    });
                }

                // Return decoded queue items
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Count = messagesList.Count,
                    QueueName = "order-transactions",
                    Messages = messagesList
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read messages from Azure Queue Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
