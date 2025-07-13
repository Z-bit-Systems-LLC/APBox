using ApBox.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add ApBox services (business logic, OSDP, etc.)
builder.Services.AddApBoxServices(builder.Configuration);

// Add API controllers
builder.Services.AddControllers();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Map API controllers
app.MapControllers();

// Map Blazor
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
