using Demo.App.Data;
using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Demo.App.Controllers;

public class OrderController : Controller
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var orders = _context.Orders.ToList();
        var customers = _context.Customers.ToList();
        var books = _context.Books.ToList();

        var transformedOrders = orders.Select(order => new OrderViewModel
        {
            Id = order.Id,
            CustomerName = customers.FirstOrDefault(c => c.Id == order.CustomerId)?.Name,
            BookTitles = order.BookIds.Select(bookId => books.FirstOrDefault(b => b.Id == bookId)?.Title).ToList(),
            BookPrices = order.BookIds.Select(bookId => books.FirstOrDefault(b => b.Id == bookId)?.Price ?? 0m).ToList(),
            OrderDate = order.OrderDate,
            OrderTotal = order.BookIds.Sum(bookId => books.FirstOrDefault(b => b.Id == bookId)?.Price ?? 0m)
        }).ToList();

        var model = Tuple.Create(transformedOrders, customers, books);
        ViewData["CurrencySymbol"] = "€";
        return View(model);
    }

    [HttpPost]
    public IActionResult AddCustomer(Customer customer)
    {
        _context.Customers.Add(customer);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult EditCustomer(Customer customer)
    {
        _context.Customers.Update(customer);
        _context.SaveChanges();
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult DeleteCustomer(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateOrderRequest request)
    {
        if (request == null || request.CustomerId <= 0 || request.BookId <= 0)
        {
            return Json(new { error = "Ongeldige invoer." });
        }

        // Create the order
        var order = new Order
        {
            CustomerId = request.CustomerId,
            BookIds = new List<int> { request.BookId },
            OrderDate = DateTime.Now
        };
        _context.Orders.Add(order);
        _context.SaveChanges();

        // Fetch customer and book details
        var customer = _context.Customers.FirstOrDefault(c => c.Id == request.CustomerId);
        var book = _context.Books.FirstOrDefault(b => b.Id == request.BookId);

        var booksList = new List<string> { book?.Title };
        var pricesList = new List<decimal> { book?.Price ?? 0m };
        var orderTotal = pricesList.Sum();

        return Json(new
        {
            id = order.Id,
            customerName = customer?.Name,
            books = booksList,
            prices = pricesList,
            orderTotal = orderTotal,
            orderDate = order.OrderDate.ToString("yyyy-MM-dd")
        });
    }
}