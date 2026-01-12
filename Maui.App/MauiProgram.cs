#nullable enable
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Devices;
using Maui.App.Services;
#if USE_EF
using Microsoft.EntityFrameworkCore;
using Maui.App.Data;
using System.IO;
#endif

namespace Maui.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Configure configuration and register ApiClient via HttpClientFactory with API key handler
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            builder.Services.AddTransient<Services.ApiKeyHandler>();

            // Resolve base URL from configuration and adapt for emulator/dev environment
            var configuredBase = builder.Configuration["Api:BaseUrl"] ?? "https://localhost:7187/";
            // If running on Android emulator, replace localhost with 10.0.2.2 and prefer http for dev
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                try
                {
                    var u = new Uri(configuredBase);
                    var host = u.Host == "localhost" ? "10.0.2.2" : u.Host;
                    var scheme = u.Scheme == "https" ? "http" : u.Scheme;
                    configuredBase = new UriBuilder(scheme, host, u.Port, u.PathAndQuery).Uri.ToString();
                }
                catch
                {
                    // ignore and use configured value if parsing fails
                }
            }

            builder.Services.AddHttpClient<Services.ApiClient>(client =>
            {
                client.BaseAddress = new Uri(configuredBase);
            })
            .AddHttpMessageHandler<Services.ApiKeyHandler>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if USE_EF
            // Register AppDbContext using builder.Services and locate Demo.App/app.db if available
            try
            {
                string? possible = null;
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "Demo.App", "app.db");
                    if (File.Exists(candidate))
                    {
                        possible = candidate;
                        break;
                    }
                    dir = dir.Parent;
                }

                if (!string.IsNullOrEmpty(possible))
                {
                    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={possible}"));
                }
                else
                {
                    var local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "app.db");
                    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={local}"));
                }
            }
            catch
            {
                // ignore if EF is not available at runtime
            }
#endif

            return builder.Build();
        }
    }
}

// Register ApiClient for REST calls to Demo.App after builder created
// Registering here requires adding namespace for the service
// Note: HttpClient registration should be done inside CreateMauiApp before Build();

