using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;

namespace CLDV7112_PROJECT2.Controllers
{
    // Controller for managing the Azure Functions monitoring and logs dashboard
    public class FunctionsController : Controller
    {
        private readonly FunctionsService _functionsService;
        private readonly EventHubService _eventHubService;
        private readonly ServiceBusService _serviceBusService;
        private readonly FileShareService _fileShareService;

        // Constructor injecting services for Azure Functions, Event Messaging, and File Storage
        public FunctionsController(
            FunctionsService functionsService,
            EventHubService eventHubService,
            ServiceBusService serviceBusService,
            FileShareService fileShareService)
        {
            _functionsService = functionsService;
            _eventHubService = eventHubService;
            _serviceBusService = serviceBusService;
            _fileShareService = fileShareService;
        }

        // Helper method to verify if the currently logged-in user is an administrator
        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // Displays the Azure Functions and cloud integration logs page
        public async Task<IActionResult> Index()
        {
            // Protect page so only logged-in store administrators can access logs
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            try
            {
                // Fetch historical system log entries recorded from Azure Functions & cloud activities
                List<LogEntry> logs = await _fileShareService.ReadLogsAsync();
                logs.Reverse(); // Display newest logs first for easy reading
                ViewBag.Logs = logs;
            }
            catch
            {
                // Fallback to empty log list if log file isn't created yet
                ViewBag.Logs = new List<LogEntry>();
            }

            return View();
        }
    }
}
