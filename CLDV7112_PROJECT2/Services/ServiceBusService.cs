using System;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace CLDV7112_PROJECT2.Services
{
    public class ServiceBusService
    {
        private readonly string _connectionString;
        private readonly string _queueOrTopicName;

        public ServiceBusService(IConfiguration configuration)
        {
            _connectionString = configuration["AzureServiceBus:ConnectionString"] ?? "";
            _queueOrTopicName = configuration["AzureServiceBus:QueueOrTopicName"] ?? "order-notifications";
        }

        public async Task<string> PublishOrderNotificationAsync(string orderId, string customerEmail, string status)
        {
            var notificationPayload = new
            {
                NotificationId = Guid.NewGuid().ToString(),
                OrderId = orderId,
                CustomerEmail = customerEmail,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            string json = JsonSerializer.Serialize(notificationPayload);

            if (string.IsNullOrEmpty(_connectionString))
            {
                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Mode = "Simulated",
                    Message = "Service Bus topic message packaged successfully. (Add AzureServiceBus:ConnectionString to publish directly to Azure Portal).",
                    Payload = notificationPayload
                });
            }

            try
            {
                await using var client = new ServiceBusClient(_connectionString);
                ServiceBusSender sender = client.CreateSender(_queueOrTopicName);
                ServiceBusMessage message = new ServiceBusMessage(json)
                {
                    Subject = "OrderNotification",
                    ContentType = "application/json"
                };
                await sender.SendMessageAsync(message);

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Mode = "Live Azure Portal",
                    Message = $"Message published live to Azure Service Bus topic/queue '{_queueOrTopicName}'.",
                    Payload = notificationPayload
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
            }
        }
    }
}
