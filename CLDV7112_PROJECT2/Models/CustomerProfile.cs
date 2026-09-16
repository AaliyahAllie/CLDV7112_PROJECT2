using System;
using Azure;
using Azure.Data.Tables;

namespace CLDV7112_PROJECT2.Models
{
    /// <summary>
    /// Represents a customer account stored in Azure Table Storage.
    /// Implements ITableEntity for direct integration with Azure Tables.
    /// </summary>
    public class CustomerProfile : ITableEntity
    {
        // PartitionKey is set to 'Customers' to group user entities together
        public string PartitionKey { get; set; } = "Customers";

        // Unique RowKey (Customer ID)
        public string RowKey { get; set; } = Guid.NewGuid().ToString("N");

        // Azure Table required timestamp and ETag tracking properties
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        // Customer's first name
        public string FirstName { get; set; } = string.Empty;

        // Customer's last name
        public string LastName { get; set; } = string.Empty;

        // Email address used for login and order notifications
        public string Email { get; set; } = string.Empty;

        // Contact phone number
        public string PhoneNumber { get; set; } = string.Empty;

        // Password hash / secret for authentication
        public string Password { get; set; } = string.Empty;

        // Account registration date
        public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

        // Helper property returning full formatted name
        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
