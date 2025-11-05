using CabBookingSystem.Models;
using CabBookingSystem.Repositories;
using CabBookingSystem.Repositories.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure MongoDB Settings from appsettings.json
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

// Register MongoDB Client as Singleton
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;

    // Validate connection string
    if (string.IsNullOrEmpty(settings.ConnectionString))
    {
        throw new ArgumentException("MongoDB connection string is not configured in appsettings.json");
    }

    Console.WriteLine($"?? Connecting to MongoDB: {settings.ConnectionString.Replace(settings.ConnectionString.Split('@')[0].Split(':')[2], "***")}");

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

        var database = client.GetDatabase(settings.DatabaseName);
        await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        Console.WriteLine("? MongoDB connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? MongoDB connection failed: {ex.Message}");
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Welcome}/{id?}");

// Add a test endpoint to check database status
app.MapGet("/db-status", async (IMongoClient client, IOptions<MongoDBSettings> settings) =>
{
    try
    {
        var database = client.GetDatabase(settings.Value.DatabaseName);
        await database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        return Results.Ok(new
        {
            status = "connected",
            database = settings.Value.DatabaseName,
            message = "MongoDB connection is working properly"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: $"MongoDB connection failed: {ex.Message}",
            statusCode: 500);
    }
});

Console.WriteLine("?? Cab Booking System starting...");
Console.WriteLine($"?? Environment: {app.Environment.EnvironmentName}");

app.Run();