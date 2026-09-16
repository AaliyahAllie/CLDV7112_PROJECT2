using System;
using Azure;
using Azure.Data.Tables;

namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Represents an inventory product item stored in Azure Table Storage.
    /// Implements ITableEntity for direct Azure Table Storage integration.
    /// </summary>
    public class Product : ITableEntity
    {
        // Category name used as PartitionKey (e.g. Electronics, Clothing)
        public string PartitionKey { get; set; } = "General";

        // Unique Product ID used as RowKey
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");

        // Azure Table required timestamp and ETag tracking properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Item display name
        public string Name { get; set; } = string.Empty;

        // Retail selling price (ZAR)
        public decimal Price { get; set; }

        // Current available stock count
        public int StockCount { get; set; }

        // Alias for views referencing StockQuantity
        public int StockQuantity
        {
            get => StockCount;
            set => StockCount = value;
        }

        // Product category grouping name
        public string Category { get; set; } = "General";

        // Product description
        public string Description { get; set; } = string.Empty;

        // Creation timestamp
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Storage URL pointing to the product image in Azure Blob Storage
        public string ImageUrl { get; set; } = string.Empty;
    }
}
