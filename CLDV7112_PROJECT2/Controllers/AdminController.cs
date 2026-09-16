using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Controllers
{
    /// <summary>
    /// Admin Portal controller: manages product inventory, customer accounts, order fulfillment statuses,
    /// order queues, and system audit logs.
    /// Triggers background Azure Functions for blob uploads, table updates, and Service Bus messages.
    /// </summary>
    public class AdminController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;
        private readonly FunctionsService _functionsService;

        public AdminController(
            TableStorageService tableStorageService,
            BlobStorageService blobStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService,
            FunctionsService functionsService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
            _functionsService = functionsService;
        }

        // Helper method verifying active Administrator session
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // GET: /Admin - Dashboard summary showing customer, product, queue, and order counts
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            var customers = await _tableStorageService.GetCustomersAsync();
            var products = await _tableStorageService.GetProductsAsync();
            var queueMessages = await _queueStorageService.GetMessagesAsync();
            var orders = await _tableStorageService.GetAllOrdersAsync();

            ViewBag.CustomerCount = customers.Count;
            ViewBag.ProductCount = products.Count;
            ViewBag.QueueMessageCount = queueMessages.Count;
            ViewBag.OrderCount = orders.Count;

            return View();
        }

        // GET: /Admin/Customers - List registered customer profiles
        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }

        // GET: /Admin/Products - Inventory management view
        [HttpGet]
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        // GET: /Admin/AddProduct - Render new product form
        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

        // POST: /Admin/AddProduct - Save new product and upload photo to Azure Blob Storage via Azure Function 2 (UploadBlob)
        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            ModelState.Remove(nameof(product.PartitionKey));
            ModelState.Remove(nameof(product.RowKey));
            ModelState.Remove(nameof(product.ImageUrl));

            if (!ModelState.IsValid) return View(product);

            product.PartitionKey = "Product";
            product.RowKey = Guid.NewGuid().ToString();

            // Upload product image to Azure Blob Storage via Azure Function 2 (UploadBlob)
            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                byte[] bytes = ms.ToArray();
                string base64 = Convert.ToBase64String(bytes);

                product.ImageUrl = await _blobStorageService.UploadBlobAsync(product.RowKey, imageFile.OpenReadStream());
                _ = _functionsService.UploadBlobAsync("product-images", $"{product.RowKey}.jpg", base64, imageFile.ContentType);
            }

            // Save product entity in Azure Tables via Azure Function 1 (StoreTableInfo)
            await _tableStorageService.UpsertProductAsync(product);
            _ = _functionsService.StoreTableInfoAsync(product);

            TempData["Success"] = $"Product '{product.Name}' added successfully!";
            return RedirectToAction("Products");
        }

        // GET: /Admin/EditProduct/{id} - Edit product form
        [HttpGet]
        public async Task<IActionResult> EditProduct(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var product = await _tableStorageService.GetProductAsync("Product", id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Admin/EditProduct - Update product info and image
        [HttpPost]
        public async Task<IActionResult> EditProduct(Product product, IFormFile imageFile)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            ModelState.Remove(nameof(product.PartitionKey));
            ModelState.Remove(nameof(product.ImageUrl));

            if (!ModelState.IsValid) return View(product);

            product.PartitionKey = "Product";

            if (imageFile != null && imageFile.Length > 0)
            {
                using var stream = imageFile.OpenReadStream();
                product.ImageUrl = await _blobStorageService.UploadBlobAsync(product.RowKey, stream);
            }

            await _tableStorageService.UpsertProductAsync(product);
            _ = _functionsService.StoreTableInfoAsync(product);

            TempData["Success"] = $"Product '{product.Name}' updated successfully!";
            return RedirectToAction("Products");
        }

        // POST: /Admin/DeleteProduct/{id} - Delete product and image blob
        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var product = await _tableStorageService.GetProductAsync("Product", id);
            if (product != null)
            {
                await _tableStorageService.DeleteProductAsync("Product", id);
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    await _blobStorageService.DeleteBlobAsync(id);
                }
            }
            TempData["Success"] = "Product deleted successfully.";
            return RedirectToAction("Products");
        }

        // GET: /Admin/Orders - View all store orders
        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var orders = await _tableStorageService.GetAllOrdersAsync();
            return View(orders);
        }

        // POST: /Admin/UpdateOrderStatus - Update order fulfillment status and publish Azure Service Bus notification
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string customerId, string orderId, string newStatus)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _tableStorageService.UpdateOrderStatusAsync(customerId, orderId, newStatus);
            await _fileShareService.AppendOrderLogAsync("INFO", $"Order status updated. OrderId: {orderId}, CustomerId: {customerId}, New Status: {newStatus}");
            
            // Publish status update notification to Azure Service Bus
            _ = _functionsService.SendServiceBusMessageAsync(new
            {
                OrderId = orderId,
                CustomerId = customerId,
                NewStatus = newStatus,
                UpdatedAt = DateTime.UtcNow
            });

            TempData["Success"] = $"Order {orderId} status updated to '{newStatus}'.";
            return RedirectToAction("Orders");
        }

        // GET: /Admin/Queue - View active messages in Azure Storage Queue
        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var messages = await _queueStorageService.GetMessagesAsync();
            return View(messages);
        }

        // POST: /Admin/ClearQueue - Clear all messages in Azure Storage Queue
        [HttpPost]
        public async Task<IActionResult> ClearQueue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _queueStorageService.ClearQueueAsync();
            TempData["Success"] = "Order Queue cleared.";
            return RedirectToAction("Queue");
        }

        // GET: /Admin/Logs - View system log files stored in Azure File Share
        [HttpGet]
        public async Task<IActionResult> Logs(string file = "system-logs.txt")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");

            ViewBag.LogFileNames = FileShareService.LogFileNames;
            ViewBag.ActiveFile = file;

            List<LogEntry> logs;
            try
            {
                logs = await _fileShareService.ReadLogFileAsync(file);
            }
            catch
            {
                logs = new List<LogEntry>();
            }

            return View(logs);
        }

        // POST: /Admin/ClearLogs - Clear log file in Azure File Share
        [HttpPost]
        public async Task<IActionResult> ClearLogs(string file = "system-logs.txt")
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            try
            {
                await _fileShareService.ClearLogFileAsync(file);
                TempData["Success"] = $"Log file '{file}' cleared.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error clearing log file: {ex.Message}";
            }
            return RedirectToAction("Logs", new { file });
        }
    }
}
