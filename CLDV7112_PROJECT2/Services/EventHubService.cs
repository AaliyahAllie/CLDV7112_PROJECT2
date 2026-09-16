using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;

namespace CLDV7112_PROJECT2.Services
{
    public class EventHubService
    {
        private readonly string _connectionString;
        private readonly string _eventHubName;

        public EventHubService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureEventHubs:ConnectionString"] ?? "";
            _eventHubName = configuration["AzureEventHubs:EventHubName"] ?? "abc-retail-events";
        }

        public async Task<string> SendClickstreamEventAsync(string userId, string action, string details)
        {
            var eventPayload = new
            {
                EventId = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(eventPayload);

            if (string.IsNullOrEmpty(_connectionString))
            {
                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Mode = "Simulated",
                    Message = "Event Hubs telemetry payload packaged successfully. (Add AzureEventHubs:ConnectionString to stream directly to Azure Portal).",
                    Payload = eventPayload
                });
            }

            try
            {
                await using var producer = new EventHubProducerClient(_connectionString, _eventHubName);
                using EventDataBatch batch = await producer.CreateBatchAsync();
                batch.TryAdd(new EventData(Encoding.UTF8.GetBytes(json)));
                await producer.SendAsync(batch);

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Mode = "Live Azure Portal",
                    Message = $"Event streamed live to Azure Event Hub '{_eventHubName}'.",
                    Payload = eventPayload
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
            }
        }
    }
}
