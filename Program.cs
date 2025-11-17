using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using CabBookingSystem.Repositories.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// ======================================================================
// Load environment variables also (Railway / Production support)
builder.Configuration.AddEnvironmentVariables();
// ======================================================================

// MVC
builder.Services.AddControllersWithViews();

// MongoDB settings from appsettings.json
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB")
);

// MongoDB Client (clean + recommended)
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.ConnectionString))
        throw new Exception("❌ MongoDB connection string missing. Check appsettings.json");

    Console.WriteLine("🔗 MongoDB client initializing...");

    return new MongoClient(settings.ConnectionString);
});

// Register MongoDB database
builder.Services.AddScoped<IMongoDatabase>(serviceProvider =>
{
    var client = serviceProvider.GetRequiredService<IMongoClient>();
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    if (string.IsNullOrEmpty(settings.DatabaseName))
        throw new Exception("❌ MongoDB DatabaseName missing from appsettings.json");

    return client.GetDatabase(settings.DatabaseName);
});

// Register repositories
builder.Services.AddScoped<ICabRepository, MongoCabRepository>();
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();
builder.Services.AddScoped<IBookingRepository, MongoBookingRepository>();
builder.Services.AddScoped<IPaymentRepository, MongoPaymentRepository>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddLogging();

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

// MVC Routing
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}"
);

// ======================================================================
// Health endpoint for Railway
app.MapGet("/health", () => new { status = "healthy", time = DateTime.UtcNow });

// ======================================================================
// MongoDB diagnostic endpoint
app.MapGet("/db-status", async (IMongoClient client, IOptions<MongoDBSettings> settings) =>
{
    try
    {
        var db = client.GetDatabase(settings.Value.DatabaseName);

        // Ping
        await db.RunCommandAsync((Command<BsonDocument>)"{ ping: 1 }");

        // Count docs
        var result = new
        {
            status = "connected",
            database = settings.Value.DatabaseName,
            collections = new
            {
                users = await db.GetCollection<BsonDocument>("Users").CountDocumentsAsync(new BsonDocument()),
                bookings = await db.GetCollection<BsonDocument>("Bookings").CountDocumentsAsync(new BsonDocument()),
                cabs = await db.GetCollection<BsonDocument>("Cabs").CountDocumentsAsync(new BsonDocument()),
                payments = await db.GetCollection<BsonDocument>("Payments").CountDocumentsAsync(new BsonDocument())
            }
        };

        return Results.Ok(result);
    }
    catch (Exception ex)
    {
        return Results.Problem($"MongoDB connection failed: {ex.Message}", statusCode: 500);
    }
});
// ======================================================================

Console.WriteLine("🚀 Cab Booking System Started Successfully");
Console.WriteLine($"📌 Environment: {app.Environment.EnvironmentName}");

app.Run();
