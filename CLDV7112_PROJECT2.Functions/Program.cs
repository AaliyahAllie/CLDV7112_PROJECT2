using Microsoft.Extensions.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

// Configure .NET 9.0 Isolated Worker Host for Azure Functions
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .Build();

// Run the serverless function worker host process
host.Run();
