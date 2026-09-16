using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Controllers
{
    public class CustomerController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueStorageService _queueStorageService;
        private readonly FileShareService _fileShareService;
        private readonly StripePaymentService _stripeService;
        private readonly FunctionsService _functionsService;
        private readonly IConfiguration _configuration;
        private const string CartSessionKey = "Cart";

        public CustomerController(
            TableStorageService tableStorageService,
            QueueStorageService queueStorageService,
            FileShareService fileShareService,
            StripePaymentService stripeService,
            FunctionsService functionsService,
            IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _queueStorageService = queueStorageService;
            _fileShareService = fileShareService;
            _stripeService = stripeService;
            _functionsService = functionsService;
            _configuration = configuration;
        }

        private bool IsCustomer() => HttpContext.Session.GetString("UserRole") == "Customer";

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        // -- Shop ----------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            var products = await _tableStorageService.GetProductsAsync();

            // Background Telemetry stream to Azure Event Hubs
            var customerId = HttpContext.Session.GetString("UserId") ?? "Guest";
            _ = _functionsService.SendEventHubTelemetryAsync(new
            {
                UserId = customerId,
                Action = "ShopProductCatalogViewed",
                Timestamp = DateTime.UtcNow
            });

            return View(products);
        }

        // -- Checkout -------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            var cart = GetCart();
            if (!cart.Any())
            {
                TempData["Error"] = "Your cart is empty. Add some products before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            var totalAmount = cart.Sum(x => x.LineTotal);
            var amountInCents = (long)(totalAmount * 100);

            string clientSecret = null;
            string pubKey = _configuration["Stripe:PublishableKey"];

            if (!string.IsNullOrWhiteSpace(pubKey) && !pubKey.Contains("YOUR_"))
            {
                try
                {
                    var paymentIntent = await _stripeService.CreatePaymentIntentAsync(amountInCents, "zar");
                    clientSecret = paymentIntent?.ClientSecret;
                }
                catch (Exception ex)
                {
                    _ = _fileShareService.AppendErrorLogAsync("WARNING", $"Stripe PaymentIntent exception: {ex.Message}");
                }
            }

            ViewBag.ClientSecret = clientSecret ?? ("demo_intent_" + Guid.NewGuid().ToString("N"));
            ViewBag.PublishableKey = string.IsNullOrWhiteSpace(pubKey) ? "pk_test_demo" : pubKey;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.IsDemoPayment = string.IsNullOrEmpty(clientSecret);

            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(string paymentIntentId)
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");

            string pId = string.IsNullOrWhiteSpace(paymentIntentId) ? ("demo_tx_" + Guid.NewGuid().ToString("N")) : paymentIntentId;

            if (!pId.StartsWith("demo_"))
            {
                try
                {
                    var paymentIntent = await _stripeService.GetPaymentIntentAsync(pId);
                    if (paymentIntent != null && paymentIntent.Status != "succeeded")
                    {
                        TempData["Error"] = "Payment was not successful. Please try again.";
                        await _fileShareService.AppendErrorLogAsync("ERROR", $"Payment failed. PaymentIntentId: {pId}, Status: {paymentIntent.Status}");
                        return RedirectToAction("Checkout");
                    }
                }
                catch (Exception ex)
                {
                    _ = _fileShareService.AppendErrorLogAsync("WARNING", $"Stripe verification fallback: {ex.Message}");
                }
            }

            var cart = GetCart();
            var customerId = HttpContext.Session.GetString("UserId");
            var customerName = HttpContext.Session.GetString("UserName");
            var customerEmail = HttpContext.Session.GetString("UserEmail");
            var totalAmount = cart.Sum(x => x.LineTotal);

            // Create an OrderEntity per cart item
            foreach (var item in cart)
            {
                var orderId = Guid.NewGuid().ToString();

                var order = new OrderEntity
                {
                    PartitionKey = customerId,
                    RowKey = orderId,
                    ProductName = item.ProductName,
                    ProductPrice = item.Price,
                    ImageUrl = item.ImageUrl,
                    Quantity = item.Quantity,
                    TotalAmount = item.LineTotal,
                    OrderDate = DateTimeOffset.UtcNow,
                    Status = "Processing",
                    PaymentStatus = "Paid",
                    PaymentIntentId = paymentIntentId,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail
                };

                // 1. Store Order in Table Storage (via Function App + Fallback)
                await _tableStorageService.UpsertOrderAsync(order);
                await _tableStorageService.UpdateProductStockAsync(item.ProductId, item.Quantity);
                _ = _functionsService.StoreTableInfoAsync(order);

                // 2. Queue transaction via Azure Function 3 (Queue Function)
                var queueMsgData = new
                {
                    OrderId = orderId,
                    CustomerName = customerName,
                    CustomerEmail = customerEmail,
                    Product = item.ProductName,
                    Quantity = item.Quantity,
                    Amount = item.LineTotal,
                    PaymentStatus = "Paid",
                    PaymentIntent = paymentIntentId,
                    Timestamp = DateTime.UtcNow
                };
                await _functionsService.WriteQueueTransactionAsync(queueMsgData);

                // 3. Write contract/invoice log via Azure Function 4 (Azure Files Function)
                string contractText = $"[INVOICE-CONTRACT] OrderId: {orderId} | Customer: {customerName} ({customerEmail}) | Item: {item.ProductName} | Total: R{item.LineTotal:F2} | Date: {DateTime.UtcNow}";
                await _functionsService.UploadAzureFileAsync("contracts", $"invoice_{orderId:N}.txt", contractText);

                // 4. Publish enterprise order notification via Azure Service Bus
                await _functionsService.SendServiceBusMessageAsync(new
                {
                    OrderId = orderId,
                    CustomerEmail = customerEmail,
                    Status = "OrderConfirmedAndQueuedForFulfillment"
                });
            }

            // Clear the cart
            HttpContext.Session.Remove(CartSessionKey);

            TempData["PaymentSuccess"] = $"Payment of R{totalAmount:F2} confirmed! {cart.Count} item(s) ordered.";
            TempData["PaymentIntentId"] = paymentIntentId;
            TempData["ItemCount"] = cart.Count.ToString();

            return RedirectToAction("PaymentSuccess");
        }

        [HttpGet]
        public IActionResult PaymentSuccess()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            return View();
        }

        [HttpGet]
        public IActionResult PaymentCancelled()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            return View();
        }

        // -- My Orders ------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            if (!IsCustomer()) return RedirectToAction("Login", "Home");
            var customerId = HttpContext.Session.GetString("UserId");
            var orders = await _tableStorageService.GetOrdersForCustomerAsync(customerId);
            return View(orders);
        }
    }
}
