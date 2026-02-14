using HomeCenter.Data;
using HomeCenter.Extensions;
using HomeCenter.Services;

// Log configuration on startup
Console.WriteLine("=== HomeCenter Configuration ===");
Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

// Load .env file for both local development and Docker
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
Console.WriteLine($"Looking for .env file at: {envPath}");

if (File.Exists(envPath))
{
    Console.WriteLine("✓ .env file found, loading...");
    DotNetEnv.Env.Load();
    Console.WriteLine("✓ .env file loaded successfully");
    
    // Log some environment variables to verify they were loaded
    var adminUsername = Environment.GetEnvironmentVariable("Admin__Username") ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME");
    var aiApiKey = Environment.GetEnvironmentVariable("AI__ApiKey") ?? Environment.GetEnvironmentVariable("AI_API_KEY");
    Console.WriteLine($"  - Admin__Username from env: {(string.IsNullOrEmpty(adminUsername) ? "NOT SET" : "SET")}");
    Console.WriteLine($"  - AI__ApiKey from env: {(string.IsNullOrEmpty(aiApiKey) ? "NOT SET" : "SET")}");
}
else
{
    Console.WriteLine("✗ .env file NOT FOUND - will use environment variables from Docker/system");
}

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;
Console.WriteLine($"Environment: {environment}");
Console.WriteLine($"ContentRootPath: {builder.Environment.ContentRootPath}");

builder.Services.AddQuizServices(builder.Configuration);

var app = builder.Build();

// Log critical configuration values (without exposing full secrets)
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    Console.WriteLine("\n=== Configuration Status ===");
    
    var hasErrors = false;
    
    // Admin credentials validation
    var adminUsername = config["Admin:Username"];
    var adminPassword = config["Admin:Password"];
    Console.WriteLine($"Admin Username: {(string.IsNullOrEmpty(adminUsername) ? "NOT SET" : adminUsername)}");
    
    if (string.IsNullOrEmpty(adminPassword))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ ERROR: Admin Password is NOT SET!");
        Console.WriteLine("   Please set Admin__Password in .env file or environment variables");
        Console.ResetColor();
        hasErrors = true;
    }
    else
    {
        Console.WriteLine($"Admin Password: SET (length: {adminPassword.Length})");
    }
    
    // AI Configuration validation
    var aiProvider = config["AI:Provider"];
    var aiApiKey = config["AI:ApiKey"];
    var aiEnabled = config["AI:Enabled"];
    var aiModel = config["AI:Model"];
    
    Console.WriteLine($"\nAI Provider: {aiProvider}");
    Console.WriteLine($"AI Enabled: {aiEnabled}");
    Console.WriteLine($"AI Model: {aiModel}");
    
    if (string.IsNullOrEmpty(aiApiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ ERROR: AI ApiKey is NOT SET!");
        Console.WriteLine("   AI features will NOT work without API key");
        Console.WriteLine("   Please set AI__ApiKey in .env file or environment variables");
        Console.ResetColor();
        hasErrors = true;
    }
    else
    {
        Console.WriteLine($"AI ApiKey: SET (length: {aiApiKey.Length}, starts with: {aiApiKey.Substring(0, Math.Min(10, aiApiKey.Length))}...)");
    }
    
    // Qwen Configuration validation
    var qwenApiKey = config["Qwen:ApiKey"];
    var qwenEnabled = config["Qwen:Enabled"];
    var isQwenEnabled = bool.TryParse(qwenEnabled, out var qwenEnabledBool) && qwenEnabledBool;
    
    Console.WriteLine($"\nQwen Enabled: {qwenEnabled}");
    
    if (isQwenEnabled && string.IsNullOrEmpty(qwenApiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("❌ ERROR: Qwen is enabled but ApiKey is NOT SET!");
        Console.WriteLine("   Qwen features will NOT work without API key");
        Console.WriteLine("   Please set Qwen__ApiKey in .env file or disable Qwen (Qwen__Enabled=false)");
        Console.ResetColor();
        hasErrors = true;
    }
    else if (!string.IsNullOrEmpty(qwenApiKey))
    {
        Console.WriteLine($"Qwen ApiKey: SET (length: {qwenApiKey.Length})");
    }
    else
    {
        Console.WriteLine("Qwen ApiKey: NOT SET (Qwen is disabled)");
    }
    
    // Connection String
    var connectionString = config.GetConnectionString("DefaultConnection");
    Console.WriteLine($"\nConnection String: {connectionString}");
    
    // Summary
    if (hasErrors)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n⚠️  WARNING: Configuration has errors! Please fix them before using the application.");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n✓ All critical configuration parameters are set correctly");
        Console.ResetColor();
    }
    
    Console.WriteLine("================================\n");
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    DatabaseMigrator.EnsureVersioningSchema(db);

    var testService = scope.ServiceProvider.GetRequiredService<ITestFileService>();
    testService.SyncTopicsFromFiles();
}

app.UseQuizPipeline();

app.Run();
