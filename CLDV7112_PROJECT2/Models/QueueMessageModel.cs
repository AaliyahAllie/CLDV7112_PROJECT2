using System;

namespace  CLDV7112_PROJECT2.Models
{
    public class QueueMessageModel
    {
        public string MessageId { get; set; }
        public string PopReceipt { get; set; }
        public string MessageText { get; set; }
        public DateTimeOffset? InsertionTime { get; set; }
        public DateTimeOffset? ExpirationTime { get; set; }
    }
}
