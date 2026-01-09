using Microsoft.Maui.Hosting;
using Microsoft.EntityFrameworkCore;
using Demo.MauiApp.Data;
using Microsoft.Maui.Storage;

namespace Demo.MauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { });
            // Configure EF Core SQLite DB in app data folder
            string dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "demo_maui.db");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

        return builder.Build();
    }
}
