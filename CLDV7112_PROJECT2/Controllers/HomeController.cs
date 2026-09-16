using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CLDV7112_PROJECT2.Controllers
{
    public class HomeController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly FileShareService _fileShareService;
        private readonly FunctionsService _functionsService;
        private readonly IConfiguration _configuration;

        public HomeController(
            TableStorageService tableStorageService,
            FileShareService fileShareService,
            FunctionsService functionsService,
            IConfiguration configuration)
        {
            _tableStorageService = tableStorageService;
            _fileShareService = fileShareService;
            _functionsService = functionsService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetProductsAsync();
            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrEmpty(usernameOrEmail) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Username/Email and Password are required.");
                return View();
            }

            // Check Admin Credentials from appsettings.json
            var adminUser = _configuration["AdminSettings:Username"] ?? "admin@abcretail.co.za";
            var adminPass = _configuration["AdminSettings:Password"] ?? "AdminPassword123!";

            if ((usernameOrEmail.Equals(adminUser, StringComparison.OrdinalIgnoreCase) || usernameOrEmail.Equals("admin", StringComparison.OrdinalIgnoreCase))
                && password == adminPass)
            {
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("UserName", "Administrator");
                await _fileShareService.AppendCustomerLogAsync("INFO", "Admin user logged in successfully.");
                return RedirectToAction("Index", "Admin");
            }

            // Check Customer in Table Storage
            var customers = await _tableStorageService.GetCustomersAsync();
            var customer = customers.Find(c => c.Email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) && c.Password == password);

            if (customer != null)
            {
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserEmail", customer.Email);
                HttpContext.Session.SetString("UserName", $"{customer.FirstName} {customer.LastName}");
                HttpContext.Session.SetString("UserId", customer.RowKey);
                await _fileShareService.AppendCustomerLogAsync("INFO", $"Customer {customer.Email} logged in successfully.");

                // Event Hubs Telemetry stream
                _ = _functionsService.SendEventHubTelemetryAsync(new
                {
                    UserId = customer.RowKey,
                    Email = customer.Email,
                    Action = "CustomerLogin",
                    Timestamp = DateTime.UtcNow
                });

                return RedirectToAction("Index", "Customer");
            }

            ModelState.AddModelError("", "Invalid credentials.");
            await _fileShareService.AppendErrorLogAsync("WARNING", $"Failed login attempt for user: {usernameOrEmail}");
            return View();
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.GetString("UserRole") != null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(CustomerProfile customer)
        {
            if (string.IsNullOrEmpty(customer.Email))
            {
                ModelState.AddModelError("Email", "Email is required.");
                return View(customer);
            }

            // Check if customer already exists
            var existing = await _tableStorageService.GetCustomersAsync();
            if (existing.Exists(c => c.Email.Equals(customer.Email, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError("Email", "A user with this email already exists.");
                return View(customer);
            }

            customer.PartitionKey = "Customer";
            customer.RowKey = Guid.NewGuid().ToString();

            ModelState.Remove(nameof(customer.Timestamp));
            ModelState.Remove(nameof(customer.ETag));
            ModelState.Remove(nameof(customer.PartitionKey));
            ModelState.Remove(nameof(customer.RowKey));

            if (ModelState.IsValid)
            {
                // Save customer profile via Azure Function 1 (Store Table Info + Fallback)
                await _tableStorageService.UpsertCustomerAsync(customer);
                _ = _functionsService.StoreTableInfoAsync(customer);

                // Event Hubs registration stream
                _ = _functionsService.SendEventHubTelemetryAsync(new
                {
                    UserId = customer.RowKey,
                    Email = customer.Email,
                    Action = "NewCustomerRegistration",
                    Timestamp = DateTime.UtcNow
                });

                await _fileShareService.AppendCustomerLogAsync("INFO", $"New customer registered: {customer.Email} ({customer.FirstName} {customer.LastName})");

                // Log in the new user
                HttpContext.Session.SetString("UserRole", "Customer");
                HttpContext.Session.SetString("UserEmail", customer.Email);
                HttpContext.Session.SetString("UserName", $"{customer.FirstName} {customer.LastName}");
                HttpContext.Session.SetString("UserId", customer.RowKey);

                return RedirectToAction("Index", "Customer");
            }

            return View(customer);
        }

        public async Task<IActionResult> Logout()
        {
            var user = HttpContext.Session.GetString("UserEmail") ?? HttpContext.Session.GetString("UserRole") ?? "Guest";
            await _fileShareService.AppendSystemLogAsync("INFO", $"User '{user}' logged out.");
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
