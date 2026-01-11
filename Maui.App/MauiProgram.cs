using Microsoft.Extensions.Logging;
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
