using Azure.Data.Tables;
using CLDV7112_PROJECT2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Services
{
    /// <summary>
    /// Service managing direct interaction with Azure Table Storage ("Customers", "Products", "Orders" tables).
    /// Handles entity queries, insertions, stock updates, and order status changes.
    /// </summary>
    public class TableStorageService
    {
        private readonly TableServiceClient _serviceClient;
        private readonly string _customerTableName = "Customers";
        private readonly string _productTableName = "Products";
        private readonly string _orderTableName = "Orders";

        public TableStorageService(string connectionString)
        {
            _serviceClient = new TableServiceClient(connectionString);
        }

        // Helper method initializing Azure Table Client and creating tables if missing
        private async Task<TableClient> GetTableClientAsync(string tableName)
        {
            var tableClient = _serviceClient.GetTableClient(tableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        // -- Customers -------------------------------------------------------------

        // Retrieves all registered customer profiles from Azure Table Storage
        public async Task<List<CustomerProfile>> GetCustomersAsync()
        {
            var tableClient = await GetTableClientAsync(_customerTableName);
            var query = tableClient.QueryAsync<CustomerProfile>(x => x.PartitionKey == "Customer" || x.PartitionKey == "Customers");
            var result = new List<CustomerProfile>();
            await foreach (var item in query) result.Add(item);
            return result;
        }

        // Fetches a single customer profile by partitionKey and rowKey
        public async Task<CustomerProfile> GetCustomerAsync(string partitionKey, string rowKey)
        {
            var tableClient = await GetTableClientAsync(_customerTableName);
            try
            {
                var response = await tableClient.GetEntityAsync<CustomerProfile>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null!;
            }
        }

        // Fetches a customer profile by email address
        public async Task<CustomerProfile> GetCustomerByEmailAsync(string email)
        {
            var customers = await GetCustomersAsync();
            return customers.FirstOrDefault(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase))!;
        }

        // Saves or updates a customer profile entity in Azure Tables
        public async Task UpsertCustomerAsync(CustomerProfile customer)
        {
            var tableClient = await GetTableClientAsync(_customerTableName);
            await tableClient.UpsertEntityAsync(customer);
        }

        // Deletes a customer profile from Azure Tables
        public async Task DeleteCustomerAsync(string partitionKey, string rowKey)
        {
            var tableClient = await GetTableClientAsync(_customerTableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // -- Products --------------------------------------------------------------

        // Retrieves all inventory products from Azure Table Storage
        public async Task<List<Product>> GetProductsAsync()
        {
            var tableClient = await GetTableClientAsync(_productTableName);
            var query = tableClient.QueryAsync<Product>(x => x.PartitionKey == "Product" || x.PartitionKey == "General");
            var result = new List<Product>();
            await foreach (var item in query) result.Add(item);
            return result;
        }

        // Fetches a single product by partitionKey and rowKey
        public async Task<Product> GetProductAsync(string partitionKey, string rowKey)
        {
            var tableClient = await GetTableClientAsync(_productTableName);
            try
            {
                var response = await tableClient.GetEntityAsync<Product>(partitionKey, rowKey);
                return response.Value;
            }
            catch
            {
                return null!;
            }
        }

        // Saves or updates a product entity in Azure Tables
        public async Task UpsertProductAsync(Product product)
        {
            var tableClient = await GetTableClientAsync(_productTableName);
            await tableClient.UpsertEntityAsync(product);
        }

        // Deletes a product item from Azure Tables
        public async Task DeleteProductAsync(string partitionKey, string rowKey)
        {
            var tableClient = await GetTableClientAsync(_productTableName);
            await tableClient.DeleteEntityAsync(partitionKey, rowKey);
        }

        // Decrements product inventory stock following a customer checkout
        public async Task UpdateProductStockAsync(string productId, int quantityToReduce)
        {
            var product = await GetProductAsync("Product", productId);
            if (product != null)
            {
                product.StockCount = Math.Max(0, product.StockCount - quantityToReduce);
                await UpsertProductAsync(product);
            }
        }

        // -- Orders ----------------------------------------------------------------

        // Fetches order history for a specific customer ID
        public async Task<List<OrderEntity>> GetOrdersForCustomerAsync(string customerId)
        {
            var tableClient = await GetTableClientAsync(_orderTableName);
            var query = tableClient.QueryAsync<OrderEntity>(x => x.PartitionKey == customerId);
            var result = new List<OrderEntity>();
            await foreach (var item in query) result.Add(item);
            return result;
        }

        // Retrieves all customer orders across the store for admin view
        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var tableClient = await GetTableClientAsync(_orderTableName);
            var query = tableClient.QueryAsync<OrderEntity>();
            var result = new List<OrderEntity>();
            await foreach (var item in query) result.Add(item);
            return result;
        }

        // Saves an order entity in Azure Tables
        public async Task UpsertOrderAsync(OrderEntity order)
        {
            var tableClient = await GetTableClientAsync(_orderTableName);
            await tableClient.UpsertEntityAsync(order);
        }

        // Updates order status (e.g. Processing -> Dispatched -> Delivered)
        public async Task UpdateOrderStatusAsync(string customerId, string orderId, string newStatus)
        {
            var tableClient = await GetTableClientAsync(_orderTableName);
            try
            {
                var response = await tableClient.GetEntityAsync<OrderEntity>(customerId, orderId);
                var order = response.Value;
                if (order != null)
                {
                    order.Status = newStatus;
                    await tableClient.UpsertEntityAsync(order);
                }
            }
            catch { }
        }
    }
}
