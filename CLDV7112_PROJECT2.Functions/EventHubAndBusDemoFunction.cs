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
    public class EventHubAndBusDemoFunction
    {
        private readonly ILogger _logger;

        public EventHubAndBusDemoFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<EventHubAndBusDemoFunction>();
        }

        [Function("SendEventHubTelemetry")]
        public async Task<HttpResponseData> SendTelemetry(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SendEventHubTelemetry")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Event Hubs telemetry stream request.");
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                string ehConnectionString = Environment.GetEnvironmentVariable("EventHubConnectionString") ?? "";
                string eventHubName = "abc-retail-events";

                if (string.IsNullOrEmpty(ehConnectionString))
                {
                    var mockResponse = req.CreateResponse(HttpStatusCode.OK);
                    mockResponse.Headers.Add("Content-Type", "application/json");
                    await mockResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Status = "Simulated / Prepared",
                        Message = "Event Hubs event batch received. (To stream live to Azure Portal, add EventHubConnectionString in appsettings/local.settings.json).",
                        HubName = eventHubName,
                        Payload = body
                    }));
                    return mockResponse;
                }

                await using var producer = new EventHubProducerClient(ehConnectionString, eventHubName);
                using EventDataBatch eventBatch = await producer.CreateBatchAsync();
                eventBatch.TryAdd(new EventData(System.Text.Encoding.UTF8.GetBytes(body)));
                await producer.SendAsync(eventBatch);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Telemetry event stream successfully sent to Azure Event Hubs via Azure Function.",
                    HubName = eventHubName
                }));
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry to Azure Event Hubs.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }

        [Function("SendServiceBusMessage")]
        public async Task<HttpResponseData> SendServiceBus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "SendServiceBusMessage")] HttpRequestData req)
        {
            _logger.LogInformation("Processing Azure Service Bus transaction request.");
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            try
            {
                string sbConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString") ?? "";
                string queueOrTopicName = "order-notifications";

                if (string.IsNullOrEmpty(sbConnectionString))
                {
                    var mockResponse = req.CreateResponse(HttpStatusCode.OK);
                    mockResponse.Headers.Add("Content-Type", "application/json");
                    await mockResponse.WriteStringAsync(JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Status = "Simulated / Prepared",
                        Message = "Service Bus message received. (To publish live to Azure Portal, add ServiceBusConnectionString in appsettings/local.settings.json).",
                        QueueOrTopic = queueOrTopicName,
                        Payload = body
                    }));
                    return mockResponse;
                }

                await using var client = new ServiceBusClient(sbConnectionString);
                ServiceBusSender sender = client.CreateSender(queueOrTopicName);
                ServiceBusMessage message = new ServiceBusMessage(body);
                await sender.SendMessageAsync(message);

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                okResponse.Headers.Add("Content-Type", "application/json");
                await okResponse.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Enterprise notification message sent to Azure Service Bus topic/queue via Azure Function.",
                    QueueOrTopic = queueOrTopicName
                }));
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Azure Service Bus.");
                var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errResponse.WriteStringAsync($"Error: {ex.Message}");
                return errResponse;
            }
        }
    }
}
