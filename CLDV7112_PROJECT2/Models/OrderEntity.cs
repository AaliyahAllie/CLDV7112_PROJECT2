using System;
using Azure;
using Azure.Data.Tables;

namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Represents a customer order record stored in Azure Table Storage.
    /// Implements ITableEntity to persist purchase data in cloud tables.
    /// </summary>
    public class OrderEntity : ITableEntity
    {
        // Customer ID used as PartitionKey to group a user's orders together
        public string PartitionKey { get; set; } = string.Empty;

        // Unique Order ID used as RowKey
        public string RowKey { get; set; } = string.Empty;

        // Azure Table required timestamp and ETag tracking properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Name of the purchased product
        public string ProductName { get; set; } = string.Empty;

        // Unit price of the item
        public decimal ProductPrice { get; set; }

        // Thumbnail image URL
        public string ImageUrl { get; set; } = string.Empty;

        // Quantity ordered
        public int Quantity { get; set; }

        // Total amount paid for this item line
        public decimal TotalAmount { get; set; }

        // Order creation timestamp
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        // Order processing status (Processing, Dispatched, Delivered)
        public string Status { get; set; } = "Processing";

        // Payment verification status (Paid, Pending)
        public string PaymentStatus { get; set; } = "Paid";

        // Stripe PaymentIntent ID or demo reference number
        public string PaymentIntentId { get; set; } = string.Empty;

        // Customer's full name
        public string CustomerName { get; set; } = string.Empty;

        // Customer's email address
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
