using System.Text;
using System.Text.Json;
using Demo.SharedModels.Models;
using MauiClient.Models;
using Microsoft.Maui.Controls;

namespace MauiClient;

public partial class IndexPage : ContentPage
{
    private readonly HttpClient _http;
    private Customer? _editingCustomer;

    public IndexPage()
    {
        InitializeComponent();
        var apiBase = Environment.GetEnvironmentVariable("API_BASE") ?? "http://10.0.2.2:5500/";
        _http = new HttpClient { BaseAddress = new Uri(apiBase) };
        _ = LoadAll();
    }

    private async Task LoadAll()
    {
        await LoadOrders();
        await LoadCustomers();
        await LoadBooks();
    }

    private async Task LoadOrders()
    {
        try
        {
            var json = await _http.GetStringAsync("api/orders");
            var orders = JsonSerializer.Deserialize<List<OrderViewModel>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            OrdersList.ItemsSource = orders;
        }
        catch (Exception) { }
    }

    private async Task LoadCustomers()
    {
        try
        {
            var json = await _http.GetStringAsync("api/customers");
            var customers = JsonSerializer.Deserialize<List<Customer>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            CustomersList.ItemsSource = customers;
        }
        catch (Exception) { }
    }

    private async Task LoadBooks()
    {
        try
        {
            var json = await _http.GetStringAsync("api/books");
            var books = JsonSerializer.Deserialize<List<Book>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            BooksList.ItemsSource = books;
        }
        catch (Exception) { }
    }

    private async void OnSaveCustomer(object sender, EventArgs e)
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
            OnNewCustomer(null, null);
        }
        else
        {
            var text = await resp.Content.ReadAsStringAsync();
            await DisplayAlert("Fout", text, "OK");
        }
    }

    private void OnNewCustomer(object sender, EventArgs e)
    {
        _editingCustomer = null;
        NameEntry.Text = string.Empty;
        EmailEntry.Text = string.Empty;
    }

    private async void OnEditCustomer(object sender, EventArgs e)
    {
        if (sender is Button b && b.CommandParameter is int id)
        {
            var resp = await _http.GetAsync($"/api/customers");
            var json = await resp.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<Customer>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var c = customers?.FirstOrDefault(x => x.Id == id);
            if (c != null)
            {
                _editingCustomer = c;
                NameEntry.Text = c.Name;
                EmailEntry.Text = c.Email;
            }
        }
    }
}
