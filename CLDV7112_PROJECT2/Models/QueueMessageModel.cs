using System;

namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Model representing a transaction message retrieved from Azure Storage Queue.
    /// </summary>
    public class QueueMessageModel
    {
        // Azure Queue message unique identifier
        public string MessageId { get; set; } = string.Empty;

        // Decoded transaction JSON message body
        public string MessageText { get; set; } = string.Empty;

        // Timestamp when the transaction was queued
        public DateTimeOffset? InsertionTime { get; set; }

        // Queue message expiration timestamp
        public DateTimeOffset? ExpirationTime { get; set; }
    }
}
