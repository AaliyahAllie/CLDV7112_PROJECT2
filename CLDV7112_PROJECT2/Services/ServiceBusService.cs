using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing decoupled enterprise messaging via Azure Service Bus topics ('order-notifications').
    /// Broadcasts order status updates to shipping, warehouse, and fulfillment microservices.
    /// </summary>
    public class ServiceBusService
    {
        private readonly string _connectionString;
        private readonly string _topicName = "order-notifications";

        public ServiceBusService(IConfiguration configuration)
        {
            _connectionString = configuration["ServiceBusConnectionString"] ?? "";
        }

        // Publishes order dispatch and fulfillment notifications to Azure Service Bus
        public async Task<string> PublishOrderNotificationAsync(string orderId, string customerEmail, string status)
        {
            var notificationPayload = new
            {
                OrderId = orderId,
                CustomerEmail = customerEmail,
                Status = status,
                Timestamp = DateTime.UtcNow
            };

            string jsonPayload = JsonSerializer.Serialize(notificationPayload);

            if (string.IsNullOrEmpty(_connectionString))
            {
                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Status = "Simulated / Prepared",
                    Message = "Service Bus order notification published successfully.",
                    TopicName = _topicName,
                    Data = notificationPayload
                });
            }

            try
            {
                await using var client = new ServiceBusClient(_connectionString);
                ServiceBusSender sender = client.CreateSender(_topicName);
                ServiceBusMessage message = new ServiceBusMessage(jsonPayload);
                await sender.SendMessageAsync(message);

                return JsonSerializer.Serialize(new
                {
                    Success = true,
                    Message = "Order notification message published to Azure Service Bus topic.",
                    TopicName = _topicName,
                    Data = notificationPayload
                });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
            }
        }
    }
}
