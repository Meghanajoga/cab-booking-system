using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using CabBookingSystem.Repositories.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
// SSL FIX - Add at the VERY TOP
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

// Bypass SSL certificate validation
ServicePointManager.ServerCertificateValidationCallback = 
    (sender, certificate, chain, sslPolicyErrors) => true;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables support for production
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MongoDB Settings from appsettings.json AND environment variables
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register MongoDB Client as Singleton
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    // Validate connection string
    if (string.IsNullOrEmpty(settings.ConnectionString))
    {
        throw new ArgumentException("MongoDB connection string is not configured. Check appsettings.json or environment variables.");
    }

    // Log connection info (mask password for security)
    var connectionInfo = settings.ConnectionString;
    try
    {
        var maskedConnection = connectionInfo.Replace(connectionInfo.Split('@')[0].Split(':')[2], "***");
        Console.WriteLine($"🔗 Connecting to MongoDB: {maskedConnection}");
    }
    catch
    {
        Console.WriteLine($"🔗 Connecting to MongoDB: [Connection string configured]");
    }

    return new MongoClient(settings.ConnectionString);
});

// Register MongoDB Database as Singleton
builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.DatabaseName))
    {
        throw new ArgumentException("MongoDB database name is not configured in appsettings.json");
    }

    return client.GetDatabase(settings.DatabaseName);
});

// Register MongoDB repositories
builder.Services.AddScoped<ICabRepository, MongoCabRepository>();
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();
builder.Services.AddScoped<IBookingRepository, MongoBookingRepository>();
builder.Services.AddScoped<IPaymentRepository, MongoPaymentRepository>();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Test MongoDB connection on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

        Console.WriteLine($"🏁 Testing MongoDB connection to database: {settings.DatabaseName}");

        var database = client.GetDatabase(settings.DatabaseName);
        await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        Console.WriteLine("✅ MongoDB connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ MongoDB connection failed: {ex.Message}");
    // Don't throw here - let the app start so you can see the error page
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // Detailed errors in development
    app.UseDeveloperExceptionPage();
}

// In production, Railway handles HTTPS redirection
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

// Health check endpoint for Railway
app.MapGet("/", () => "🚗 Cab Booking System is running! Visit /Home/Welcome to get started.");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
app.MapGet("/wake-up", () => "✅ Service is awake and ready!");

// Add a test endpoint to check database status
app.MapGet("/db-status", async (IMongoClient client, IOptions<MongoDBSettings> settings) =>
{
    try
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");

        // Test if we can read from collections
        var db = client.GetDatabase(settings.Value.DatabaseName);
        var usersCount = await db.GetCollection<BsonDocument>("Users").CountDocumentsAsync(new BsonDocument());
        var bookingsCount = await db.GetCollection<BsonDocument>("Bookings").CountDocumentsAsync(new BsonDocument());

        return Results.Ok(new
        {
            status = "connected",
            database = settings.Value.DatabaseName,
            message = "MongoDB connection is working properly",
            collections = new
            {
                users = usersCount,
                bookings = bookingsCount,
                cabs = await db.GetCollection<BsonDocument>("Cabs").CountDocumentsAsync(new BsonDocument()),
                payments = await db.GetCollection<BsonDocument>("Payments").CountDocumentsAsync(new BsonDocument())
            },
            environment = app.Environment.EnvironmentName
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"MongoDB connection failed: {ex.Message}",
            statusCode: 500);
    }
});

Console.WriteLine("🚀 Cab Booking System starting...");
Console.WriteLine($"📍 Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"📍 Application Name: {builder.Environment.ApplicationName}");
Console.WriteLine($"📍 Content Root Path: {builder.Environment.ContentRootPath}");

app.Run();