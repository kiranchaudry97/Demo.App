using Demo.App.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add API controllers
builder.Services.AddControllers();

// Add CORS policy for MAUI local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Configure SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register RabbitMQ publisher
builder.Services.AddSingleton<Demo.App.Services.IRabbitMqPublisher, Demo.App.Services.RabbitMqPublisher>();
// Register simple RabbitMQ consumer background service (logs messages)
builder.Services.AddHostedService<Demo.App.Services.RabbitMqConsumer>();

// Disable configuration-based endpoint overrides
builder.WebHost.UseSetting("urls", ""); // Clear IConfiguration bindings

// Configure Kestrel to use explicit endpoints
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureEndpointDefaults(listenOptions =>
    {
        listenOptions.UseConnectionLogging(); // Optional: Add logging for connections
    });

    // Explicitly define endpoints
    serverOptions.Listen(System.Net.IPAddress.Loopback, 7187, listenOptions =>
    {
        listenOptions.UseHttps(); // Use HTTPS for secure connections
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCors("AllowAll");
// Validate API key for API endpoints
app.UseMiddleware<Demo.App.Middleware.ApiKeyMiddleware>();
app.UseAuthorization();

// Map controllers (MVC + API)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Order}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
