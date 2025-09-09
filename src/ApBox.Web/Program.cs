using ApBox.Web.Services;
using ApBox.Web.Hubs;
using ApBox.Web.Constants;
using ApBox.Core.Data;
using ApBox.Core.Logging;
using ApBox.Core.Services.Infrastructure;
using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using Blazored.LocalStorage;
using Microsoft.Extensions.Hosting.Systemd;

var builder = WebApplication.CreateBuilder(args);

// Add systemd integration
builder.Services.AddSystemd();

// Configure Kestrel for external access in production only
if (!builder.Environment.IsDevelopment())
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Bind to all network interfaces on port 5000 for HTTP
        options.ListenAnyIP(5000);
        
        // Only configure HTTPS if certificate is available
        var httpsPort = builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT");
        if (httpsPort.HasValue)
        {
            options.ListenAnyIP(httpsPort.Value, listenOptions =>
            {
                listenOptions.UseHttps();
            });
        }
    });
}

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add Blazorise
builder.Services
    .AddBlazorise( options =>
    {
        options.Immediate = true;
    } )
    .AddBootstrap5Providers()
    .AddFontAwesomeIcons();

// Add SignalR
builder.Services.AddSignalR();

// Add ApBox services (business logic, OSDP, etc.)
builder.Services.AddApBoxServices(builder.Configuration);

// Add Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// Add card number format service
builder.Services.AddScoped<CardNumberFormatService>();

// Add HttpClient for Blazor components
builder.Services.AddHttpClient();

// Add API controllers
builder.Services.AddControllers();

// Add CORS for external API access
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExternalSystems", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ApBox API",
        Version = "v1",
        Description = "REST API for ApBox Card Reader Management System",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Z-bit",
            Email = "support@z-bit.com"
        }
    });
    
    // Include XML comments
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    options.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.InitializeApBoxDatabaseAsync();
}

// Add custom log provider to capture application logs
var logService = app.Services.GetRequiredService<ILogService>();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
loggerFactory.AddProvider(new ApBoxLogProvider(logService));

// Configuration change notifications are now handled directly via dependency injection

// Notify systemd that the service is ready (after all initialization)
var hostEnvironment = app.Services.GetRequiredService<IHostEnvironment>();
if (!hostEnvironment.IsDevelopment())
{
    // This tells systemd the service is ready and healthy
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStarted.Register(() =>
    {
        // The systemd integration will automatically notify systemd
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application startup completed - systemd notified");
    });
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only redirect to HTTPS if HTTPS is configured
if (builder.Configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT").HasValue)
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();

// Enable CORS
app.UseCors("AllowExternalSystems");

// Map API controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<NotificationHub>(HubConstants.NotificationHubUrl);
app.MapHub<PacketTraceHub>("/packettracehub");

// Map Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
