using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CLDV7112_PROJECT2.Services;

namespace CLDV7112_PROJECT2.Controllers
{
    public class FunctionsController : Controller
    {
        private readonly FunctionsService _functionsService;
        private readonly EventHubService _eventHubService;
        private readonly ServiceBusService _serviceBusService;

        public FunctionsController(
            FunctionsService functionsService,
            EventHubService eventHubService,
            ServiceBusService serviceBusService)
        {
            _functionsService = functionsService;
            _eventHubService = eventHubService;
            _serviceBusService = serviceBusService;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        public IActionResult Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestStoreTableInfo(string tableName, string partitionKey, string rowKey, string name, string email)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            var data = new
            {
                TableName = string.IsNullOrWhiteSpace(tableName) ? "Customers" : tableName,
                PartitionKey = string.IsNullOrWhiteSpace(partitionKey) ? "Customers" : partitionKey,
                RowKey = string.IsNullOrWhiteSpace(rowKey) ? Guid.NewGuid().ToString("N") : rowKey,
                Name = name ?? "Sample User",
                Email = email ?? "sample@abcretail.co.za",
                DateCreated = DateTime.UtcNow
            };

            var responseJson = await _functionsService.StoreTableInfoAsync(data);
            ViewBag.ResultTitle = "Azure Table Storage Function Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestUploadBlob(string containerName, string blobName, string textContent)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            string content = string.IsNullOrWhiteSpace(textContent) ? "ABC Retail Blob Content Created via Azure Function." : textContent;
            string base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content));
            string cName = string.IsNullOrWhiteSpace(containerName) ? "product-images" : containerName;
            string bName = string.IsNullOrWhiteSpace(blobName) ? $"log_{Guid.NewGuid():N}.txt" : blobName;

            var responseJson = await _functionsService.UploadBlobAsync(cName, bName, base64);
            ViewBag.ResultTitle = "Azure Blob Storage Function Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestWriteQueueTransaction(string customerId, string orderId, decimal amount)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            var txData = new
            {
                TransactionId = Guid.NewGuid().ToString("N"),
                CustomerId = string.IsNullOrWhiteSpace(customerId) ? "CUST001" : customerId,
                OrderId = string.IsNullOrWhiteSpace(orderId) ? "ORD-1001" : orderId,
                Amount = amount <= 0 ? 499.99m : amount,
                Timestamp = DateTime.UtcNow
            };

            var responseJson = await _functionsService.WriteQueueTransactionAsync(txData);
            ViewBag.ResultTitle = "Azure Queue Storage Write Function Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestReadQueueTransaction()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            var responseJson = await _functionsService.ReadQueueTransactionAsync();
            ViewBag.ResultTitle = "Azure Queue Storage Read Function Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestUploadAzureFile(string shareName, string fileName, string fileContent)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            string sName = string.IsNullOrWhiteSpace(shareName) ? "contracts" : shareName;
            string fName = string.IsNullOrWhiteSpace(fileName) ? $"contract_{Guid.NewGuid():N}.txt" : fileName;
            string content = string.IsNullOrWhiteSpace(fileContent) ? "ABC Retail Supplier Agreement & Proof of Delivery Document." : fileContent;

            var responseJson = await _functionsService.UploadAzureFileAsync(sName, fName, content);
            ViewBag.ResultTitle = "Azure Files Storage Function Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestEventHubs(string userId, string action, string details)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            string uId = string.IsNullOrWhiteSpace(userId) ? "USER_123" : userId;
            string act = string.IsNullOrWhiteSpace(action) ? "AddToCart" : action;
            string det = string.IsNullOrWhiteSpace(details) ? "Product #402 viewed and added to shopping cart" : details;

            var responseJson = await _eventHubService.SendClickstreamEventAsync(uId, act, det);
            ViewBag.ResultTitle = "Azure Event Hubs Real-Time Streaming Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TestServiceBus(string orderId, string customerEmail, string status)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            string oId = string.IsNullOrWhiteSpace(orderId) ? "ORD-8842" : orderId;
            string email = string.IsNullOrWhiteSpace(customerEmail) ? "customer@abcretail.co.za" : customerEmail;
            string stat = string.IsNullOrWhiteSpace(status) ? "PaymentVerified" : status;

            var responseJson = await _serviceBusService.PublishOrderNotificationAsync(oId, email, stat);
            ViewBag.ResultTitle = "Azure Service Bus Enterprise Messaging Result";
            ViewBag.ResultJson = responseJson;
            return View("Index");
        }
    }
}
