using Microsoft.Maui.Controls;
using Demo.SharedModels.Models;

namespace Demo.MauiApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // Load sample data from the Demo.App API if available, otherwise use local sample
        _ = LoadBooksAsync();

        async Task LoadBooksAsync()
        {
            try
            {
                using var client = new HttpClient { BaseAddress = new Uri("http://localhost:7187") };
                var booksFromApi = await client.GetFromJsonAsync<List<Book>>("/api/books");
                if (booksFromApi != null && booksFromApi.Count > 0)
                {
                    BooksListView.ItemsSource = booksFromApi;
                    return;
                }
            }
            catch { /* ignore and fall back to local sample */ }

            var books = new List<Book>
            {
                new Book { Id = 1, Title = "C# in Depth", Author = "Jon Skeet", Price = 49.99m },
                new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", Price = 39.99m }
            };

            BooksListView.ItemsSource = books;
        }
    }
}
