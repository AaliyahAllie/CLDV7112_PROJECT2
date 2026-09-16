using CLDV7112_PROJECT2.Services;
using Stripe;
using System;

// Initialize ASP.NET Core 9.0 Web Application builder
var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers with Razor views and HTTP Context Accessor
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Configure user session state for cart, authentication, and role management
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure HSTS for HTTPS security enforcement
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = false;
});

// Configure Stripe Payment Gateway secret key from appsettings/cloud environment
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Retrieve Azure Storage Connection String from environment configuration
var connectionString = builder.Configuration["AzureStorage:ConnectionString"] ?? "UseDevelopmentStorage=true";

// Register Azure Storage Services as Singleton services across the app lifecycle
builder.Services.AddSingleton(new TableStorageService(connectionString));
builder.Services.AddSingleton(new BlobStorageService(connectionString));
builder.Services.AddSingleton(new QueueStorageService(connectionString));
builder.Services.AddSingleton(new FileShareService(connectionString));
builder.Services.AddSingleton<StripePaymentService>();

// Register Project 2 Azure Functions & Cloud Messaging Services
builder.Services.AddSingleton<FunctionsService>();
builder.Services.AddSingleton<EventHubService>();
builder.Services.AddSingleton<ServiceBusService>();

var app = builder.Build();

// Configure environment exception handlers
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Enforce modern HTTP security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.MapStaticAssets();

// Define default MVC routing pattern
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Launch the web application
app.Run();
