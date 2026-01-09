using Demo.App.Data;
using Demo.SharedModels.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers;

public class CustomerController : Controller
{
    private readonly AppDbContext _context;

    public CustomerController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var customers = _context.Customers.ToList();
        var books = _context.Books.ToList();
        var orders = _context.Orders.ToList();
        ViewBag.Books = books;
        ViewBag.Orders = orders;
        ViewData["CurrencySymbol"] = "€";
        return View(customers);
    }

    public IActionResult Select(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return NotFound();
        TempData["SelectedCustomer"] = customer.Id;
        TempData["PromptMessage"] = "Klant geselecteerd! Kies nu een boek om te bestellen.";
        return RedirectToAction("Index", "Order");
    }

    public IActionResult Order(int bookId)
    {
        if (TempData["SelectedCustomer"] == null)
        {
            TempData["OrderMessage"] = "Selecteer eerst een klant.";
            return RedirectToAction("Index");
        }

        var customerId = (int)TempData["SelectedCustomer"];
        var order = new Order
        {
            CustomerId = customerId,
            BookIds = new List<int> { bookId },
            OrderDate = DateTime.UtcNow
        };

        _context.Orders.Add(order);
        _context.SaveChanges();

        TempData["OrderMessage"] = "Het boek is succesvol besteld!";
        return RedirectToAction("Index", "Order");
    }

    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public JsonResult Add([FromForm] Customer customer)
    {
        Console.WriteLine("Received customer data:");
        Console.WriteLine("Id: {0}, Name: {1}, Email: {2}", customer?.Id, customer?.Name, customer?.Email);

        if (customer == null)
        {
            Console.WriteLine("Received null customer object.");
            return Json(new { error = "Invalid customer data." });
        }

        if (string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
        {
            Console.WriteLine("Validation failed: Name = {0}, Email = {1}", customer.Name, customer.Email);
            return Json(new { error = "Name and Email are required." });
        }

        if (customer.Id == 0)
        {
            _context.Customers.Add(customer);
        }
        else
        {
            _context.Customers.Update(customer);
        }

        _context.SaveChanges();

        var updatedCustomers = _context.Customers.ToList();
        Console.WriteLine("Updated customers:");
        updatedCustomers.ForEach(c => Console.WriteLine("Id: {0}, Name: {1}, Email: {2}", c.Id, c.Name, c.Email));

        return Json(updatedCustomers);
    }

    public IActionResult Edit(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost]
    public IActionResult Edit(Customer customer)
    {
        _context.Customers.Update(customer);
        _context.SaveChanges();
        return RedirectToAction("Index", "Order");
    }

    public IActionResult Delete(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            _context.SaveChanges();
        }
        return RedirectToAction("Index", "Order");
    }

    [HttpPost]
    public JsonResult Save([FromBody] Customer? customer)
    {
        Console.WriteLine("Save called. Customer: {0}", customer is null ? "null" : $"Id={customer.Id}, Name={customer.Name}, Email={customer.Email}");

        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
        {
            Console.WriteLine("Validation failed in Save: Name = {0}, Email = {1}", customer?.Name, customer?.Email);
            return Json(new { error = "Ongeldige invoer." });
        }

        if (customer.Id > 0)
        {
            var existingCustomer = _context.Customers.FirstOrDefault(c => c.Id == customer.Id);
            if (existingCustomer != null)
            {
                existingCustomer.Name = customer.Name;
                existingCustomer.Email = customer.Email;
                _context.SaveChanges();
            }
        }
        else
        {
            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        Console.WriteLine("Customer saved successfully in Save: Name = {0}, Email = {1}", customer.Name, customer.Email);

        return Json(new { id = customer.Id, name = customer.Name, email = customer.Email });
    }
}