using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Demo.SharedModels.Models;
using Microsoft.Maui.Controls;

namespace MauiClient;

public partial class CustomersPage : ContentPage
{
    private readonly HttpClient _http;

    public CustomersPage()
    {
        InitializeComponent();
        // Determine API base from environment (launchSettings) or fallback to Android emulator host
        var apiBase = Environment.GetEnvironmentVariable("API_BASE") ?? "http://10.0.2.2:5500/";
        _http = new HttpClient { BaseAddress = new Uri(apiBase) };
        _ = LoadCustomers();
    }

    private async Task LoadCustomers()
    {
        try
        {
            var customers = await _http.GetFromJsonAsync<List<Customer>>("api/customers");
            CustomersList.ItemsSource = customers;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Fout", ex.Message, "OK");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Fout", "Naam en email zijn verplicht.", "OK");
            return;
        }

        var customer = new Customer { Name = name, Email = email };
        var json = JsonSerializer.Serialize(customer);
        var resp = await _http.PostAsync("api/customers", new StringContent(json, Encoding.UTF8, "application/json"));
        if (resp.IsSuccessStatusCode)
        {
            await LoadCustomers();
            OnNewClicked(null, null);
        }
        else
        {
            var text = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Fout", text, "OK");
        }
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var resp = await _http.DeleteAsync($"api/customers/{id}");
            if (resp.IsSuccessStatusCode)
            {
                await LoadCustomers();
            }
            else
            {
                await DisplayAlert("Fout", "Kon niet verwijderen.", "OK");
            }
        }
    }

    private void OnNewClicked(object sender, EventArgs e)
    {
        NameEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
    }
}
