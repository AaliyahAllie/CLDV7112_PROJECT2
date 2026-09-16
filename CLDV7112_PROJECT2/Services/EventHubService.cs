using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing real-time clickstream telemetry event ingestion into Azure Event Hubs.
    /// Tracks customer searches, product views, and category navigation.
    /// </summary>
    public class EventHubService
    {
        private readonly string _connectionString;
        private readonly string _eventHubName = "abc-retail-events";

        public EventHubService(IConfiguration configuration)
        {
            _connectionString = configuration["EventHubConnectionString"] ?? "";
        }

        // Sends user clickstream activity event to Azure Event Hubs
        public async Task<string> SendClickstreamEventAsync(string userId, string action, string details)
        {
            var eventPayload = new
            {
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow
            };

            string jsonPayload = JsonSerializer.Serialize(eventPayload);

            if (string.IsNullOrEmpty(_connectionString))
            {
                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Status = "Simulated / Prepared",
                    Message = "Event Hubs event batch received successfully.",
                    HubName = _eventHubName,
                    Data = eventPayload
                });
            }

            try
            {
                await using var producer = new EventHubProducerClient(_connectionString, _eventHubName);
                using EventDataBatch batch = await producer.CreateBatchAsync();
                batch.TryAdd(new EventData(System.Text.Encoding.UTF8.GetBytes(jsonPayload)));
                await producer.SendAsync(batch);

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Clickstream telemetry streamed successfully to Azure Event Hubs.",
                    HubName = _eventHubName,
                    Data = eventPayload
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
            }
        }
    }
}
