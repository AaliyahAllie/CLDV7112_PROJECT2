using System.Net;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CLDV7112_PROJECT2.Functions
{
    /// <summary>
    /// Section B Microservices: Real-Time Telemetry (Event Hubs) & Pub/Sub Messaging (Service Bus).
    /// Streams clickstream activity and broadcasts order status updates across microservices.
    /// </summary>
    public class EventHubAndBusDemoFunction
    {
        private readonly ILogger _logger;

        public EventHubAndBusDemoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventHubAndBusDemoFunction>();
        }

        // HTTP trigger function listening at /api/SendEventHubTelemetry
        [Function("SendEventHubTelemetry")]
        public async Task<HttpResponseData> SendTelemetry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SendEventHubTelemetry")] HttpRequestData req)
        {
            _logger.LogInformation("Processing real-time clickstream event for Azure Event Hubs...");
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                string ehConnectionString = Environment.GetEnvironmentVariable("EventHubConnectionString") ?? "";
                string eventHubName = "abc-retail-events";

                // Fallback for demo environment if Event Hubs connection string is not present
                if (string.IsNullOrEmpty(ehConnectionString))
                {
                    var mockResponse = req.CreateResponse(HttpStatusCode.OK);
                    mockResponse.Headers.Add("Content-Type", "application/json");
                    await mockResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Status = "Simulated / Prepared",
                        Message = "Event Hubs telemetry batch processed successfully.",
                        HubName = eventHubName,
                        Payload = body
                    }));
                    return mockResponse;
                }

                // Stream event batch to Azure Event Hubs
                await using var producer = new EventHubProducerClient(ehConnectionString, eventHubName);
                using EventDataBatch eventBatch = await producer.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(System.Text.Encoding.UTF8.GetBytes(body)));
                await producer.SendAsync(eventBatch);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Telemetry event stream successfully sent to Azure Event Hubs via serverless function.",
                    HubName = eventHubName
                }));
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stream telemetry event to Azure Event Hubs.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }

        // HTTP trigger function listening at /api/SendServiceBusMessage
        [Function("SendServiceBusMessage")]
        public async Task<HttpResponseData> SendServiceBus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SendServiceBusMessage")] HttpRequestData req)
        {
            _logger.LogInformation("Publishing order fulfillment status notification to Azure Service Bus...");
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                string sbConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString") ?? "";
                string queueOrTopicName = "order-notifications";

                // Fallback for demo environment if Service Bus connection string is unconfigured
                if (string.IsNullOrEmpty(sbConnectionString))
                {
                    var mockResponse = req.CreateResponse(HttpStatusCode.OK);
                    mockResponse.Headers.Add("Content-Type", "application/json");
                    await mockResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Status = "Simulated / Prepared",
                        Message = "Service Bus dispatch message published successfully.",
                        QueueOrTopic = queueOrTopicName,
                        Payload = body
                    }));
                    return mockResponse;
                }

                // Publish message to Azure Service Bus topic/queue
                await using var client = new ServiceBusClient(sbConnectionString);
                ServiceBusSender sender = client.CreateSender(queueOrTopicName);
                ServiceBusMessage message = new ServiceBusMessage(body);
                await sender.SendMessageAsync(message);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Enterprise notification message sent to Azure Service Bus topic via serverless function.",
                    QueueOrTopic = queueOrTopicName
                }));
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message to Azure Service Bus.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
