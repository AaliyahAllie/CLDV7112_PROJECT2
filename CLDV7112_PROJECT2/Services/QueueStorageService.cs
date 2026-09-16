using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using CLDV7112_PROJECT2.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing direct interaction with Azure Storage Queue ('order-transactions').
    /// Pushes, retrieves, and clears transaction queue messages.
    /// </summary>
    public class QueueStorageService
    {
        private readonly QueueServiceClient _queueServiceClient;
        private readonly string _queueName = "order-transactions";

        public QueueStorageService(string connectionString)
        {
            _queueServiceClient = new QueueServiceClient(connectionString);
        }

        // Helper method returning QueueClient and ensuring the queue exists
        private async Task<QueueClient> GetQueueClientAsync()
        {
            var queueClient = _queueServiceClient.GetQueueClient(_queueName);
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }

        // Pushes a base64 encoded transaction message to Azure Queue Storage
        public async Task SendMessageAsync(string messageText)
        {
            var queueClient = await GetQueueClientAsync();
            string base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageText));
            await queueClient.SendMessageAsync(base64Message);
        }

        // Retrieves active queue messages from Azure Storage Queue
        public async Task<List<QueueMessageModel>> GetMessagesAsync(int maxMessages = 10)
        {
            var queueClient = await GetQueueClientAsync();
            QueueMessage[] messages = await queueClient.ReceiveMessagesAsync(maxMessages);
            var result = new List<QueueMessageModel>();

            foreach (var message in messages)
            {
                string text;
                try
                {
                    var bytes = Convert.FromBase64String(message.MessageText);
                    text = System.Text.Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    text = message.MessageText;
                }

                result.Add(new QueueMessageModel
                {
                    MessageId = message.MessageId,
                    MessageText = text,
                    InsertionTime = message.InsertedOn,
                    ExpirationTime = message.ExpiresOn
                });
            }

            return result;
        }

        // Clears all messages from the transaction queue
        public async Task ClearQueueAsync()
        {
            var queueClient = await GetQueueClientAsync();
            await queueClient.ClearMessagesAsync();
        }
    }
}
