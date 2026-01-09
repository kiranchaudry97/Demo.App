using Demo.App.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseAuthorization();

// Map the default route to the consolidated Index page
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Order}/{action=Index}/{id?}");

app.Run();
