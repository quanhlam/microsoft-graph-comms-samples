using ArtyVoiceBot.Models;
using ArtyVoiceBot.Services;
using Microsoft.Graph.Communications.Common.Telemetry;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel with certificate for HTTPS
builder.WebHost.ConfigureKestrel((context, serverOptions) =>
{
    var botConfig = context.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>();
    
    // HTTP endpoint on port 9442
    serverOptions.ListenAnyIP(9442, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
    
    // HTTPS endpoint on port 9441 with certificate
    serverOptions.ListenAnyIP(9441, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        
        // Load certificate from Windows certificate store
        if (!string.IsNullOrEmpty(botConfig?.CertificateThumbprint))
        {
            using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                botConfig.CertificateThumbprint,
                validOnly: false);
            
            if (certs.Count > 0)
            {
                listenOptions.UseHttps(certs[0]);
                Console.WriteLine($"✅ HTTPS configured with certificate: {botConfig.CertificateThumbprint}");
            }
            else
            {
                Console.WriteLine($"⚠️ Certificate not found: {botConfig.CertificateThumbprint}");
                Console.WriteLine("   Using development certificate instead");
                listenOptions.UseHttps(); // Falls back to dev cert
            }
            
            store.Close();
        }
        else
        {
            // Use ASP.NET Core development certificate
            listenOptions.UseHttps();
            Console.WriteLine("⚠️ Using development certificate for HTTPS");
        }
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Arty Voice Bot API",
        Version = "v1",
        Description = "REST API for controlling Arty bot's Teams meeting voice capture"
    });
});

// Configure settings from appsettings.json
var botConfig = builder.Configuration.GetSection("BotConfiguration").Get<BotConfiguration>() 
    ?? throw new InvalidOperationException("BotConfiguration section is missing from appsettings.json");

var audioSettings = builder.Configuration.GetSection("AudioSettings").Get<AudioSettings>() 
    ?? new AudioSettings();

var pythonBackendSettings = builder.Configuration.GetSection("PythonBackend").Get<PythonBackendSettings>() 
    ?? new PythonBackendSettings();

// Register configuration as singletons
builder.Services.AddSingleton(botConfig);
builder.Services.AddSingleton(audioSettings);
builder.Services.AddSingleton(pythonBackendSettings);

// Register Graph Logger
builder.Services.AddSingleton<IGraphLogger>(sp =>
{
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    return new SimpleGraphLogger("ArtyVoiceBot", loggerFactory);
});

// Register HTTP client for webhook service
builder.Services.AddHttpClient("PythonBackend", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register services
builder.Services.AddSingleton<AudioCaptureService>();
builder.Services.AddSingleton<WebhookService>();
builder.Services.AddSingleton<ArtyBotService>();

// Configure CORS if needed for Python backend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Initialize the bot service
var botService = app.Services.GetRequiredService<ArtyBotService>();
try
{
    botService.Initialize();
    app.Logger.LogInformation("✅ Arty Voice Bot initialized successfully");
    app.Logger.LogInformation($"📞 Bot Name: {botConfig.BotName}");
    app.Logger.LogInformation($"🌐 Service DNS: {botConfig.ServiceDnsName}");
    app.Logger.LogInformation($"🎤 Audio Output: {audioSettings.OutputFolder}");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "❌ Failed to initialize Arty Voice Bot");
    // Don't throw - allow app to start so we can see error messages
}

app.Logger.LogInformation("🚀 Arty Voice Bot is running!");
app.Logger.LogInformation($"📝 Swagger UI: http://localhost:{builder.Configuration["ASPNETCORE_URLS"]?.Split(':').Last() ?? "5000"}/swagger");

app.Run();

/// <summary>
/// Graph Logger implementation using Microsoft.Graph.Communications.Common
/// Inherits from the SDK's GraphLogger base class and binds to ILogger
/// </summary>
public class SimpleGraphLogger : Microsoft.Graph.Communications.Common.Telemetry.GraphLogger
{
    public SimpleGraphLogger(string component, ILoggerFactory loggerFactory) 
        : base(component, redirectToTrace: false)
    {
        // Bind the GraphLogger to ASP.NET Core's ILogger
        this.BindToILoggerFactory(loggerFactory);
    }
}

