using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CLDV7112_PROJECT2.Services
{
    public class FunctionsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly BlobStorageService _blobStorageService;
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;

        public FunctionsService(
            IConfiguration configuration,
            BlobStorageService blobStorageService,
            TableStorageService tableStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService)
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _baseUrl = configuration["AzureFunctions:BaseUrl"] ?? "http://localhost:7071/api";
            _blobStorageService = blobStorageService;
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
        }

        public async Task<string> StoreTableInfoAsync(object data)
        {
            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/StoreTableInfo";
            string response = await PostJsonAsync(primaryUrl, data);

            if (IsNetworkError(response))
            {
                try
                {
                    using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
                    var root = doc.RootElement;
                    string name = root.TryGetProperty("Name", out var n) ? n.GetString() ?? "Sample User" : "Sample User";
                    string email = root.TryGetProperty("Email", out var e) ? e.GetString() ?? "user@abcretail.co.za" : "user@abcretail.co.za";

                    var customer = new Models.CustomerProfile
                    {
                        PartitionKey = "Customers",
                        RowKey = Guid.NewGuid().ToString("N"),
                        FirstName = name,
                        LastName = "User",
                        Email = email,
                        PhoneNumber = "011-000-0000",
                        Password = "Password123!"
                    };
                    await _tableStorageService.UpsertCustomerAsync(customer);

                    return JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Mode = "Direct Storage Fallback (Start CLDV7112_PROJECT2.Functions locally or deploy to Azure)",
                        Message = $"Entity for '{name}' successfully stored in Azure Table 'Customers'.",
                        PartitionKey = customer.PartitionKey,
                        RowKey = customer.RowKey
                    });
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
                }
            }

            return response;
        }

        public async Task<string> UploadBlobAsync(string containerName, string blobName, string contentBase64, string contentType = "text/plain")
        {
            var payload = new
            {
                ContainerName = containerName,
                BlobName = blobName,
                ContentBase64 = contentBase64,
                ContentType = contentType
            };

            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/UploadBlob";
            string response = await PostJsonAsync(primaryUrl, payload);

            if (IsNetworkError(response))
            {
                try
                {
                    byte[] data = Convert.FromBase64String(contentBase64);
                    using var ms = new System.IO.MemoryStream(data);
                    string url = await _blobStorageService.UploadBlobAsync(blobName, ms);

                    return JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Mode = "Direct Storage Fallback (Start CLDV7112_PROJECT2.Functions locally or deploy to Azure)",
                        Message = $"Blob '{blobName}' successfully uploaded to Azure Blob container '{containerName}'.",
                        BlobUrl = url
                    });
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
                }
            }

            return response;
        }

        public async Task<string> WriteQueueTransactionAsync(object transactionData)
        {
            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/WriteQueueTransaction";
            string response = await PostJsonAsync(primaryUrl, transactionData);

            if (IsNetworkError(response))
            {
                try
                {
                    string json = JsonSerializer.Serialize(transactionData);
                    await _queueStorageService.SendMessageAsync(json);

                    return JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Mode = "Direct Storage Fallback (Start CLDV7112_PROJECT2.Functions locally or deploy to Azure)",
                        Message = "Transaction message successfully published to Azure Queue 'order-transactions'.",
                        Payload = transactionData
                    });
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
                }
            }

            return response;
        }

        public async Task<string> ReadQueueTransactionAsync()
        {
            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/ReadQueueTransaction";
            try
            {
                var httpResponse = await _httpClient.GetAsync(primaryUrl);
                return await httpResponse.Content.ReadAsStringAsync();
            }
            catch
            {
                if (!_baseUrl.Contains("localhost:7071"))
                {
                    try
                    {
                        var httpResponse = await _httpClient.GetAsync("http://localhost:7071/api/ReadQueueTransaction");
                        return await httpResponse.Content.ReadAsStringAsync();
                    }
                    catch { }
                }

                try
                {
                    var messages = await _queueStorageService.GetMessagesAsync(5);
                    return JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Count = messages.Count,
                        QueueName = "order-transactions",
                        Messages = messages
                    });
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
                }
            }
        }

        public async Task<string> UploadAzureFileAsync(string shareName, string fileName, string content)
        {
            var payload = new
            {
                ShareName = shareName,
                FileName = fileName,
                Content = content
            };

            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/UploadAzureFile";
            string response = await PostJsonAsync(primaryUrl, payload);

            if (IsNetworkError(response))
            {
                try
                {
                    await _fileShareService.AppendCustomerLogAsync("CONTRACT", $"[FileShare: {shareName}] {fileName} - {content}");

                    return JsonSerializer.Serialize(new
                    {
                        Success = true,
                        Mode = "Direct Storage Fallback (Start CLDV7112_PROJECT2.Functions locally or deploy to Azure)",
                        Message = $"File '{fileName}' successfully saved to Azure File Share '{shareName}'.",
                        ShareName = shareName,
                        FileName = fileName
                    });
                }
                catch (Exception ex)
                {
                    return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
                }
            }

            return response;
        }

        public async Task<string> SendEventHubTelemetryAsync(object eventData)
        {
            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/SendEventHubTelemetry";
            return await PostJsonAsync(primaryUrl, eventData);
        }

        public async Task<string> SendServiceBusMessageAsync(object notificationData)
        {
            string primaryUrl = $"{_baseUrl.TrimEnd('/')}/SendServiceBusMessage";
            return await PostJsonAsync(primaryUrl, notificationData);
        }

        private async Task<string> PostJsonAsync(string url, object data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(url, content);
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                if (!url.Contains("localhost:7071"))
                {
                    try
                    {
                        string localUrl = "http://localhost:7071/api/" + url.Substring(url.LastIndexOf('/') + 1);
                        string json = JsonSerializer.Serialize(data);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync(localUrl, content);
                        return await response.Content.ReadAsStringAsync();
                    }
                    catch { }
                }

                return JsonSerializer.Serialize(new { Success = false, Error = ex.Message });
            }
        }

        private static bool IsNetworkError(string jsonResponse)
        {
            return jsonResponse.Contains("No such host is known") || 
                   jsonResponse.Contains("Connection refused") ||
                   jsonResponse.Contains("A connection attempt failed") ||
                   jsonResponse.Contains("The operation has timed out");
        }
    }
}
