using Microsoft.Maui.Controls;

namespace MauiClient;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new IndexPage());
    }
}
