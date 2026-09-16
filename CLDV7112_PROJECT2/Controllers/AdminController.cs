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

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // -- Dashboard ------------------------------------------------------------

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

        // -- Customers ------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Customers()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var customers = await _tableStorageService.GetCustomersAsync();
            return View(customers);
        }

        // -- Inventory (Products) -------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            return View();
        }

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

            // Upload image via Azure Function 2 (Blob Function + Fallback)
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

            await _tableStorageService.UpsertProductAsync(product);
            _ = _functionsService.StoreTableInfoAsync(product);

            TempData["Success"] = $"Product '{product.Name}' added successfully!";
            return RedirectToAction("Products");
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(string id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var product = await _tableStorageService.GetProductAsync("Product", id);
            if (product == null) return NotFound();
            return View(product);
        }

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

        // -- Orders ---------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Orders()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var orders = await _tableStorageService.GetAllOrdersAsync();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string customerId, string orderId, string newStatus)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _tableStorageService.UpdateOrderStatusAsync(customerId, orderId, newStatus);
            await _fileShareService.AppendOrderLogAsync("INFO", $"Order status updated. OrderId: {orderId}, CustomerId: {customerId}, New Status: {newStatus}");
            
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

        // -- Queue & Logs ---------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Queue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            var messages = await _queueStorageService.GetMessagesAsync();
            return View(messages);
        }

        [HttpPost]
        public async Task<IActionResult> ClearQueue()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Home");
            await _queueStorageService.ClearQueueAsync();
            TempData["Success"] = "Order Queue cleared.";
            return RedirectToAction("Queue");
        }

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
