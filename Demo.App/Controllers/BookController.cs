using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Demo.App.Controllers;

public class BookController : Controller
{
    private static readonly List<Book> Books = new()
    {
        new Book { Id = 1, Title = "C# in Depth", Author = "Jon Skeet", Price = 49.99m },
        new Book { Id = 2, Title = "Clean Code", Author = "Robert C. Martin", Price = 39.99m },
        new Book { Id = 3, Title = "The Pragmatic Programmer", Author = "Andrew Hunt", Price = 44.99m }
    };

    public IActionResult Index()
    {
        ViewData["CurrencySymbol"] = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
        return View(Books);
    }

    public IActionResult Details(int id)
    {
        var book = Books.FirstOrDefault(b => b.Id == id);
        if (book == null) return NotFound();
        ViewData["CurrencySymbol"] = CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol;
        return View(book);
    }
}