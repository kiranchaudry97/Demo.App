using Maui.App.Models;
#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
#if USE_EF
using Microsoft.EntityFrameworkCore;
using Maui.App.Data;
#endif

namespace Maui.App;

public partial class MainPage : ContentPage
{
    private readonly List<Customer> _customers = new();
    private readonly List<Book> _books = new();
    private readonly List<Order> _orders = new();
    private readonly Services.ApiClient? _api;
    private int _nextCustomerId = 1;
    private int _nextBookId = 1;
    private int _nextOrderId = 1;
#if USE_EF
    private readonly AppDbContext? _db;
#endif

    public MainPage()
    {
        InitializeComponent();
        // try to resolve AppDbContext from MAUI DI container
#if !USE_EF
        // try to resolve ApiClient from DI for REST backend
        try
        {
            _api = Application.Current?.Handler?.MauiContext?.Services?.GetService<Services.ApiClient>();
        }
        catch
        {
            _api = null;
        }
    #endif
#if USE_EF
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            _db = services?.GetService<AppDbContext>();
        }
        catch
        {
            _db = null;
        }

#endif
    }

    private bool _isLoaded;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded) return;
        _isLoaded = true;

#if USE_EF
        if (_db != null)
        {
            await LoadFromDatabaseAsync();
        }
        else
        {
            SeedData();
        }
#else
        // if ApiClient available, load from backend
        if (_api != null)
        {
            await LoadFromApiAsync();
        }
        else
        {
            SeedData();
        }
#endif

        RefreshAllViews();
    }

    private void SeedData()
    {
        _customers.Add(new Customer { Id = _nextCustomerId++, Name = "Jan Jansen", Email = "jan.jansen@example.com" });
        _customers.Add(new Customer { Id = _nextCustomerId++, Name = "Piet Pietersen", Email = "piet.pietersen@example.com" });

        _books.Add(new Book { Id = _nextBookId++, Title = "C# in Depth", Author = "Jon Skeet", Price = 49.99m });
        _books.Add(new Book { Id = _nextBookId++, Title = "Clean Code", Author = "Robert C. Martin", Price = 39.99m });
        _books.Add(new Book { Id = _nextBookId++, Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Price = 44.99m });

        // sample order
        _orders.Add(new Order { Id = _nextOrderId++, CustomerId = 1, BookIds = new List<int> { 1, 2 }, OrderDate = DateTime.Now });
    }

    private void LoadFromDatabase()
    {
        #if USE_EF
        throw new NotSupportedException("Use LoadFromDatabaseAsync when USE_EF is defined.");
        #else
        _customers.Clear();
        _books.Clear();
        _orders.Clear();

        // Load from seed (no DB)
        SeedData();
        #endif
    }

#if USE_EF
    private async Task LoadFromDatabaseAsync()
    {
        if (_db == null) return;

        _customers.Clear();
        _books.Clear();
        _orders.Clear();

        var customers = await _db.Customers.ToListAsync();
        var books = await _db.Books.ToListAsync();
        var orders = await _db.Orders.ToListAsync();

        _customers.AddRange(customers);
        _books.AddRange(books);
        _orders.AddRange(orders);

        _nextCustomerId = (_customers.Count > 0) ? _customers.Max(c => c.Id) + 1 : 1;
        _nextBookId = (_books.Count > 0) ? _books.Max(b => b.Id) + 1 : 1;
        _nextOrderId = (_orders.Count > 0) ? _orders.Max(o => o.Id) + 1 : 1;
    }
#endif

    private async Task LoadFromApiAsync()
    {
        if (_api == null) return;
        try
        {
            var customers = await _api.GetCustomersAsync();
            var books = await _api.GetBooksAsync();
            var orders = await _api.GetOrdersAsync();

            _customers.Clear();
            _books.Clear();
            _orders.Clear();

            _customers.AddRange(customers);
            _books.AddRange(books);
            _orders.AddRange(orders);

            _nextCustomerId = (_customers.Count > 0) ? _customers.Max(c => c.Id) + 1 : 1;
            _nextBookId = (_books.Count > 0) ? _books.Max(b => b.Id) + 1 : 1;
            _nextOrderId = (_orders.Count > 0) ? _orders.Max(o => o.Id) + 1 : 1;
        }
        catch
        {
            // ignore API failures and fallback to seeded data
            SeedData();
        }
    }

    private void RefreshAllViews()
    {
        CustomersView.ItemsSource = _customers;
        BooksView.ItemsSource = _books;

        var orderViewModels = _orders.Select(o => new Models.OrderViewModel
        {
            Id = o.Id,
            CustomerName = _customers.FirstOrDefault(c => c.Id == o.CustomerId)?.Name ?? "",
            BookTitles = o.BookIds.Select(id => _books.FirstOrDefault(b => b.Id == id)?.Title ?? string.Empty).ToList(),
            BookPrices = o.BookIds.Select(id => _books.FirstOrDefault(b => b.Id == id)?.Price ?? 0m).ToList(),
            OrderDate = o.OrderDate,
            OrderTotal = o.BookIds.Sum(id => _books.FirstOrDefault(b => b.Id == id)?.Price ?? 0m)
        }).ToList();

        OrdersView.ItemsSource = orderViewModels;

        // Provide object lists to pickers and use ItemDisplayBinding in XAML to show Name/Title
        OrderCustomerPicker.ItemsSource = _customers;
        OrderBookPicker.ItemsSource = _books;

        // No filters: nothing to populate
    }

    private (int id, string display)? ParsePickerItem(string? item)
    {
        if (string.IsNullOrWhiteSpace(item)) return null;
        var parts = item.Split(new[] { ':' }, 2);
        if (parts.Length < 2) return null;
        if (!int.TryParse(parts[0].Trim(), out var id)) return null;
        return (id, parts[1].Trim());
    }

    // ParsePickerItem removed — pickers use object SelectedItem (Customer / Book)

    private async void OnSaveCustomerClicked(object sender, EventArgs e)
    {
        var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
        var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
        var name = detailName?.Text?.Trim();
        var email = detailEmail?.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            DisplayAlert("Fout", "Voer een geldige naam en e-mail in.", "OK");
            return;
        }

        // If editing (Id stored in the button's BindingContext), reuse editing pattern: we check if there's a selected customer in CollectionView
        var selected = CustomersView.SelectedItem as Customer;
        #if USE_EF
        if (_db != null)
        {
            if (selected != null)
            {
                var dbCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == selected.Id);
                if (dbCustomer != null)
                {
                    dbCustomer.Name = name;
                    dbCustomer.Email = email;
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var customer = new Customer { Name = name, Email = email };
                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            await LoadFromDatabaseAsync();
        }
        else
        {
            if (selected != null)
            {
                selected.Name = name;
                selected.Email = email;
            }
            else
            {
                var customer = new Customer { Id = _nextCustomerId++, Name = name, Email = email };
                _customers.Add(customer);
            }
        }
        #else
        if (_api != null)
        {
            if (selected != null)
            {
                selected.Name = name;
                selected.Email = email;
                await _api.UpdateCustomerAsync(selected);
                await LoadFromApiAsync();
            }
            else
            {
                var create = new Customer { Name = name, Email = email };
                await _api.CreateCustomerAsync(create);
                await LoadFromApiAsync();
            }
        }
        else
        {
            if (selected != null)
            {
                selected.Name = name;
                selected.Email = email;
            }
            else
            {
                var customer = new Customer { Id = _nextCustomerId++, Name = name, Email = email };
                _customers.Add(customer);
            }
        }
        #endif

        // Clear inputs
        if (detailName != null) detailName.Text = string.Empty;
        if (detailEmail != null) detailEmail.Text = string.Empty;
        CustomersView.SelectedItem = null;

        RefreshAllViews();
    }

    private void OnEditCustomerClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var customer = _customers.FirstOrDefault(c => c.Id == id);
            if (customer != null)
            {
                var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
                var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
                if (detailName != null) detailName.Text = customer.Name;
                if (detailEmail != null) detailEmail.Text = customer.Email;
                CustomersView.SelectedItem = customer;
            }
        }
    }

    private async void OnCustomerSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as Customer;
        var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
        var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
        if (selected != null)
        {
            // populate detail entry fields
            if (detailName != null) detailName.Text = selected.Name;
            if (detailEmail != null) detailEmail.Text = selected.Email;
            // no separate overview labels; detail entries are editable
            // also select this customer in the order picker so seller can immediately choose a book
            try
            {
                OrderCustomerPicker.SelectedItem = _customers.FirstOrDefault(c => c.Id == selected.Id);
                // refresh book picker state based on newly selected customer
                OnOrderPickerChanged(null, EventArgs.Empty);

                // scroll to orders area and focus book picker so seller can pick book immediately
                try
                {
                    // small delay to allow layout update
                    await Task.Delay(100);
                    var scroll = this.FindByName<ScrollView>("MainScroll");
                    var orders = this.FindByName<Frame>("OrdersFrame");
                    if (scroll != null && orders != null)
                    {
                        await scroll.ScrollToAsync(orders, ScrollToPosition.Start, true);
                    }

                    var bookPicker = this.FindByName<Picker>("OrderBookPicker");
                    if (bookPicker != null && bookPicker.IsEnabled)
                    {
                        bookPicker.Focus();
                    }
                }
                catch
                {
                    // ignore scroll/focus errors
                }
            }
            catch
            {
                // ignore any selection issues (defensive)
            }
        }
        else
        {
            if (detailName != null) detailName.Text = string.Empty;
            if (detailEmail != null) detailEmail.Text = string.Empty;
            // clear detail entries
            // clear order pickers when no customer selected
            OrderCustomerPicker.SelectedItem = null;
            OrderBookPicker.SelectedItem = null;
        }
    }

    private async void OnSaveDetailsClicked(object sender, EventArgs e)
    {
        var selected = CustomersView.SelectedItem as Customer;
        var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
        var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
        var name = detailName?.Text?.Trim();
        var email = detailEmail?.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Fout", "Vul naam en e-mail in om op te slaan.", "OK");
            return;
        }

        // Create new
        if (selected == null)
        {
#if USE_EF
            if (_db != null)
            {
                var newDbCustomer = new Customer { Name = name!, Email = email! };
                _db.Customers.Add(newDbCustomer);
                await _db.SaveChangesAsync();
                await LoadFromDatabaseAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    RefreshAllViews();
                    var created = _customers.FirstOrDefault(c => c.Name == name && c.Email == email);
                    if (created != null) CustomersView.SelectedItem = created;
                });
                return;
            }
#endif

            if (_api != null)
            {
                var create = new Customer { Name = name!, Email = email! };
                await _api.CreateCustomerAsync(create);
                await LoadFromApiAsync();
                RefreshAllViews();
                CustomersView.SelectedItem = _customers.FirstOrDefault(c => c.Name == name && c.Email == email);
                return;
            }

            var createdMem = new Customer { Id = _nextCustomerId++, Name = name!, Email = email! };
            _customers.Add(createdMem);
            RefreshAllViews();
            CustomersView.SelectedItem = createdMem;
            return;
        }

        // Update existing
        selected.Name = name ?? selected.Name;
        selected.Email = email ?? selected.Email;

#if USE_EF
        if (_db != null)
        {
            var dbCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == selected.Id);
            if (dbCustomer != null)
            {
                dbCustomer.Name = selected.Name;
                dbCustomer.Email = selected.Email;
                await _db.SaveChangesAsync();
                await LoadFromDatabaseAsync();
                MainThread.BeginInvokeOnMainThread(RefreshAllViews);
                return;
            }
        }
#endif

        if (_api != null)
        {
            await _api.UpdateCustomerAsync(selected);
            await LoadFromApiAsync();
            MainThread.BeginInvokeOnMainThread(RefreshAllViews);
            return;
        }

        // fallback in-memory
        RefreshAllViews();
    }

    private void OnClearDetailsClicked(object sender, EventArgs e)
    {
        var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
        var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
        if (detailName != null) detailName.Text = string.Empty;
        if (detailEmail != null) detailEmail.Text = string.Empty;
    }

    private async void OnDeleteCustomerClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var confirm = await DisplayAlert("Bevestig", "Weet je zeker dat je deze klant wilt verwijderen?", "Ja", "Nee");
            if (!confirm) return;
            #if USE_EF
            if (_db != null)
            {
                var dbCustomer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id);
                if (dbCustomer != null)
                {
                    _db.Customers.Remove(dbCustomer);
                    await _db.SaveChangesAsync();
                    await LoadFromDatabaseAsync();
                    RefreshAllViews();
                    return;
                }
            }
            #endif

            if (_api != null)
            {
                await _api.DeleteCustomerAsync(id);
                await LoadFromApiAsync();
                RefreshAllViews();
                return;
            }

            var cust = _customers.FirstOrDefault(c => c.Id == id);
            if (cust != null)
            {
                _customers.Remove(cust);
                RefreshAllViews();
            }
        }
    }

    private void OnNewCustomerClicked(object sender, EventArgs e)
    {
        var detailName = this.FindByName<Entry>("DetailCustomerNameEntry");
        var detailEmail = this.FindByName<Entry>("DetailCustomerEmailEntry");
        if (detailName != null) detailName.Text = string.Empty;
        if (detailEmail != null) detailEmail.Text = string.Empty;
        CustomersView.SelectedItem = null;
    }

    private void OnOrderPickerChanged(object sender, EventArgs e)
    {
        var selectedCustomer = OrderCustomerPicker.SelectedItem as Customer;
        var bookPicker = this.FindByName<Picker>("OrderBookPicker");
        if (selectedCustomer == null)
        {
            // no customer selected -> disable book picker and reset to full list
            if (bookPicker != null)
            {
                bookPicker.IsEnabled = false;
                OrderBookPicker.ItemsSource = _books;
                OrderBookPicker.SelectedItem = null;
            }
            OrderActionButton.IsEnabled = false;
            return;
        }

        // customer selected -> enable book picker and filter out books already ordered by this customer
        if (bookPicker != null) bookPicker.IsEnabled = true;

        var customerId = selectedCustomer.Id;
        var orderedBookIds = _orders.Where(o => o.CustomerId == customerId).SelectMany(o => o.BookIds).Distinct().ToHashSet();
        var availableBooks = _books.Where(b => !orderedBookIds.Contains(b.Id)).ToList();

        if (availableBooks.Count == 0)
        {
            // no available books left
            OrderBookPicker.ItemsSource = new List<Book>();
            OrderBookPicker.SelectedItem = null;
            OrderActionButton.IsEnabled = false;
            return;
        }

        OrderBookPicker.ItemsSource = availableBooks;

        var selectedBook = OrderBookPicker.SelectedItem as Book;
        OrderActionButton.IsEnabled = (selectedBook != null);
    }

    // Book add UI removed; books are displayed read-only to match Demo.App layout

    private async void OnCreateOrderClicked(object sender, EventArgs e)
    {
        var customer = OrderCustomerPicker.SelectedItem as Customer;
        var book = OrderBookPicker.SelectedItem as Book;
        if (customer == null)
        {
            await DisplayAlert("Fout", "Selecteer eerst een klant.", "OK");
            return;
        }
        if (book == null)
        {
            await DisplayAlert("Fout", "Selecteer eerst een boek.", "OK");
            return;
        }
        if (customer == null || book == null)
        {
            DisplayAlert("Fout", "Geselecteerde items niet gevonden.", "OK");
            return;
        }

        // If OrderActionButton has CommandParameter set to an order id, update that order
        if (OrderActionButton?.CommandParameter is int existingOrderId)
        {
            #if USE_EF
            if (_db != null)
            {
                var dbOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == existingOrderId);
                if (dbOrder != null)
                {
                    dbOrder.CustomerId = customer.Id;
                    dbOrder.BookIds = new List<int> { book.Id };
                    dbOrder.OrderDate = DateTime.Now;
                    dbOrder.Name = customer.Name;
                    await _db.SaveChangesAsync();
                    await LoadFromDatabaseAsync();
                }
            }
            else
            {
                var ord = _orders.FirstOrDefault(o => o.Id == existingOrderId);
                if (ord != null)
                {
                    ord.CustomerId = customer.Id;
                    ord.BookIds = new List<int> { book.Id };
                    ord.OrderDate = DateTime.Now;
                }
            }
            #else
            var ord = _orders.FirstOrDefault(o => o.Id == existingOrderId);
            if (_api != null)
            {
                var update = new Order { CustomerId = customer.Id, BookIds = new List<int> { book.Id }, OrderDate = DateTime.Now };
                await _api.UpdateOrderAsync(existingOrderId, update);
                await LoadFromApiAsync();
            }
            else
            {
                if (ord != null)
                {
                    ord.CustomerId = customer.Id;
                    ord.BookIds = new List<int> { book.Id };
                    ord.OrderDate = DateTime.Now;
                }
            }
            #endif

            // Reset action button and pickers
            OrderActionButton.Text = "Bestel";
            OrderActionButton.CommandParameter = null;
            OrderCustomerPicker.SelectedItem = null;
            OrderBookPicker.SelectedItem = null;
        }
        else
        {
                // Create new order
                if (_api != null)
                {
                    var newOrder = new Order { CustomerId = customer.Id, BookIds = new List<int> { book.Id }, OrderDate = DateTime.Now };
                    await _api.CreateOrderAsync(newOrder);
                    await LoadFromApiAsync();
                }
            else
            {
                #if USE_EF
                if (_db != null)
                {
                    var order = new Order { CustomerId = customer.Id, BookIds = new List<int> { book.Id }, OrderDate = DateTime.Now, Name = customer.Name };
                    _db.Orders.Add(order);
                    await _db.SaveChangesAsync();
                    await LoadFromDatabaseAsync();
                }
                else
                {
                    var order = new Order { Id = _nextOrderId++, CustomerId = customer.Id, BookIds = new List<int> { book.Id }, OrderDate = DateTime.Now };
                    _orders.Add(order);
                }
                #else
                var order = new Order { Id = _nextOrderId++, CustomerId = customer.Id, BookIds = new List<int> { book.Id }, OrderDate = DateTime.Now };
                _orders.Add(order);
                #endif
            }
        }

        RefreshAllViews();
    }

    private void OnEditOrderClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                // preselect pickers (select by object)
                var cust = _customers.FirstOrDefault(c => c.Id == order.CustomerId);
                var bk = _books.FirstOrDefault(b => order.BookIds.Contains(b.Id));
                if (cust != null) OrderCustomerPicker.SelectedItem = cust;
                if (bk != null) OrderBookPicker.SelectedItem = bk;
                OrderActionButton.Text = "Bijwerken";
                OrderActionButton.CommandParameter = order.Id;
            }
        }
    }

    private async void OnDeleteOrderClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is int id)
        {
            var confirm = await DisplayAlert("Bevestig", "Weet je zeker dat je deze bestelling wilt verwijderen?", "Ja", "Nee");
            if (!confirm) return;

            #if USE_EF
            if (_db != null)
            {
                var dbOrder = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
                if (dbOrder != null)
                {
                    _db.Orders.Remove(dbOrder);
                    await _db.SaveChangesAsync();
                    await LoadFromDatabaseAsync();
                    RefreshAllViews();
                    return;
                }
            }
            #endif

            if (_api != null)
            {
                await _api.DeleteOrderAsync(id);
                await LoadFromApiAsync();
                RefreshAllViews();
                return;
            }

            var ord = _orders.FirstOrDefault(o => o.Id == id);
            if (ord != null)
            {
                _orders.Remove(ord);
                RefreshAllViews();
            }
        }
    }

    private void OnCancelOrderEditClicked(object sender, EventArgs e)
    {
        OrderCustomerPicker.SelectedItem = null;
        OrderBookPicker.SelectedItem = null;
        OrderActionButton.Text = "Bestel";
        OrderActionButton.CommandParameter = null;
    }

    private void OnCounterClicked(object? sender, EventArgs e)
    {
        // keep original counter behavior as example
    }
}
