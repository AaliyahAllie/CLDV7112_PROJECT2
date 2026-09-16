using System.Net;
using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    public class ProcessQueueTransactionFunction
    {
        private readonly ILogger _logger;

        public ProcessQueueTransactionFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessQueueTransactionFunction>();
        }

        [Function("WriteQueueTransaction")]
        public async Task<HttpResponseData> WriteTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "WriteQueueTransaction")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Write Queue Transaction request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body cannot be empty.");
                return badResponse;
            }

            try
            {
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                var queueClient = new QueueClient(connString, "order-transactions");
                await queueClient.CreateIfNotExistsAsync();

                string base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(requestBody));
                var receipt = await queueClient.SendMessageAsync(base64Message);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Transaction message successfully published to Azure Queue 'order-transactions' via Azure Function.",
                    MessageId = receipt.Value.MessageId,
                    PopReceipt = receipt.Value.PopReceipt,
                    ExpirationTime = receipt.Value.ExpirationTime
                }));

                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing to Azure Queue Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }

        [Function("ReadQueueTransaction")]
        public async Task<HttpResponseData> ReadTransaction(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ReadQueueTransaction")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Read Queue Transaction request.");

            try
            {
                string connString = Environment.GetEnvironmentVariable("AzureStorageConnectionString")
                                   ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                                   ?? "UseDevelopmentStorage=true";

                var queueClient = new QueueClient(connString, "order-transactions");
                await queueClient.CreateIfNotExistsAsync();

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
                _logger.LogError(ex, "Error reading from Azure Queue Storage.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
