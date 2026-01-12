using Maui.App.Services;

namespace Maui.App;

public partial class App : Application

{

    public App(RabbitMqService rabbitMqService)

    {

        InitializeComponent();

        MainPage = new AppShell();

        Task.Run(() =>

        {
            try
            {
                rabbitMqService.Send("MAUI app gestart ✅");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RabbitMQ error: {ex}");
            }
        });
    }
}

