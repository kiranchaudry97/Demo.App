using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers;

public class OrderController : Controller
{
    private static readonly List<Order> Orders = new();

    public IActionResult Create(int bookId)
    {
        // Placeholder logic to simulate selecting a customer and book
        var customer = new Customer { Id = 1, Name = "Jan Jansen", Email = "jan.jansen@example.com" };
        var book = new Book { Id = bookId, Title = "Sample Book", Author = "Author", Price = 10.0m };

        var order = new Order
        {
            Id = Orders.Count + 1,
            CustomerId = customer.Id,
            BookIds = new List<int> { book.Id },
            OrderDate = DateTime.UtcNow
        };

        Orders.Add(order);
        return RedirectToAction("Confirm", new { orderId = order.Id });
    }

    public IActionResult Confirm(int orderId)
    {
        var order = Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null) return NotFound();
        return View(order);
    }
}