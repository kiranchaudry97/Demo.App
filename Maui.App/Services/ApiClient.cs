using System.Net.Http.Json;
using Maui.App.Models;

namespace Maui.App.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public async Task<List<Customer>> GetCustomersAsync()
    {
        return await _http.GetFromJsonAsync<List<Customer>>("api/customers") ?? new List<Customer>();
    }

    public async Task<List<Book>> GetBooksAsync()
    {
        return await _http.GetFromJsonAsync<List<Book>>("api/books") ?? new List<Book>();
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        return await _http.GetFromJsonAsync<List<Order>>("api/orders") ?? new List<Order>();
    }

    public async Task<Customer?> CreateCustomerAsync(Customer c)
    {
        var resp = await _http.PostAsJsonAsync("api/customers", c);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Customer>();
    }

    public async Task<bool> UpdateCustomerAsync(Customer c)
    {
        if (c == null) return false;
        var resp = await _http.PutAsJsonAsync($"api/customers/{c.Id}", c);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/customers/{id}");
        return resp.IsSuccessStatusCode;
    }

    public async Task<Book?> CreateBookAsync(Book b)
    {
        var resp = await _http.PostAsJsonAsync("api/books", b);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Book>();
    }

    public async Task<bool> UpdateBookAsync(Book b)
    {
        if (b == null) return false;
        var resp = await _http.PutAsJsonAsync($"api/books/{b.Id}", b);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteBookAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/books/{id}");
        return resp.IsSuccessStatusCode;
    }

    public async Task<Order?> CreateOrderAsync(Order o)
    {
        var resp = await _http.PostAsJsonAsync("api/orders", o);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<Order>();
    }

    public async Task<bool> UpdateOrderAsync(int id, Order o)
    {
        if (o == null) return false;
        var resp = await _http.PutAsJsonAsync($"api/orders/{id}", o);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteOrderAsync(int id)
    {
        var resp = await _http.DeleteAsync($"api/orders/{id}");
        return resp.IsSuccessStatusCode;
    }
}
