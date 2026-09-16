using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;

namespace CLDV7112_PROJECT2.Controllers
{
    /// <summary>
    /// Manages shopping cart operations (view cart, add items, update quantities, remove items).
    /// Stores cart state in encrypted HTTP session storage for seamless shopping.
    /// </summary>
    public class CartController : Controller
    {
        private const string CartSessionKey = "Cart";
        private readonly TableStorageService _tableStorageService;
        private readonly FunctionsService _functionsService;

        public CartController(TableStorageService tableStorageService, FunctionsService functionsService)
        {
            _tableStorageService = tableStorageService;
            _functionsService = functionsService;
        }

        // Retrieves the list of cart items stored in session
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson)) return new List<CartItem>();
            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        // Saves current cart state back to session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }

        // GET: /Cart - Renders the shopping cart page
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // POST: /Cart/AddToCart - Adds a product item to the shopping cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(string partitionKey, string rowKey, int quantity = 1)
        {
            var product = await _tableStorageService.GetProductAsync(partitionKey, rowKey);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("Index", "Customer");
            }

            var cart = GetCart();
            var existingItem = cart.FirstOrDefault(x => x.ProductId == rowKey);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.RowKey,
                    ProductName = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Category = product.Category,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            TempData["Success"] = $"Added '{product.Name}' to your shopping cart!";

            // Stream user activity telemetry to Azure Event Hubs invisibly
            _ = _functionsService.SendEventHubTelemetryAsync(new
            {
                User = HttpContext.Session.GetString("UserName") ?? "Visitor",
                Action = "AddToCart",
                Product = product.Name,
                Category = product.Category,
                Timestamp = DateTime.UtcNow
            });

            return RedirectToAction("Index");
        }

        // POST: /Cart/UpdateQuantity - Changes the quantity of an item in the cart
        [HttpPost]
        public IActionResult UpdateQuantity(string productId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // POST: /Cart/RemoveFromCart - Deletes an item from the cart
        [HttpPost]
        public IActionResult RemoveFromCart(string productId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["Success"] = $"Removed '{item.ProductName}' from your cart.";
            }
            return RedirectToAction("Index");
        }
    }
}
