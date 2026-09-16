using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CLDV7112_PROJECT2.Models;
using CLDV7112_PROJECT2.Services;

namespace CLDV7112_PROJECT2.Controllers
{
    public class FunctionsController : Controller
    {
        private readonly FunctionsService _functionsService;
        private readonly EventHubService _eventHubService;
        private readonly ServiceBusService _serviceBusService;
        private readonly FileShareService _fileShareService;

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

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        public async Task<IActionResult> Index()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Home");

            try
            {
                List<LogEntry> logs = await _fileShareService.ReadLogsAsync();
                logs.Reverse(); // Display newest logs first
                ViewBag.Logs = logs;
            }
            catch
            {
                ViewBag.Logs = new List<LogEntry>();
            }

            return View();
        }
    }
}
