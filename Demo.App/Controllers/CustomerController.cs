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
        if (!(TempData["SelectedCustomer"] is int customerId))
        {
            TempData["OrderMessage"] = "Selecteer eerst een klant.";
            return RedirectToAction("Index");
        }
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

    // API endpoint for MAUI/native clients to fetch customers
    [HttpGet]
    [Route("api/customers")]
    public JsonResult GetCustomers()
    {
        var customers = _context.Customers.ToList();
        return Json(customers);
    }

    // API endpoint for MAUI/native clients to create or update a customer
    [HttpPost]
    [Route("api/customers")]
    public JsonResult CreateOrUpdateCustomer([FromBody] Customer? customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
        {
            return Json(new { error = "Invalid input." });
        }

        if (customer.Id > 0)
        {
            var existing = _context.Customers.FirstOrDefault(c => c.Id == customer.Id);
            if (existing != null)
            {
                existing.Name = customer.Name;
                existing.Email = customer.Email;
                _context.SaveChanges();
            }
        }
        else
        {
            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        return Json(new { id = customer.Id, name = customer.Name, email = customer.Email });
    }

    // API endpoint for MAUI/native clients to delete a customer
    [HttpDelete]
    [Route("api/customers/{id}")]
    public JsonResult DeleteCustomerApi(int id)
    {
        var customer = _context.Customers.FirstOrDefault(c => c.Id == id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            _context.SaveChanges();
            return Json(new { success = true });
        }

        return Json(new { error = "Not found" });
    }

    [HttpPost]
    public async Task<JsonResult> Save()
    {
        // Read raw request body to help debugging
        Request.EnableBuffering();
        using var reader = new System.IO.StreamReader(Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        Request.Body.Position = 0;
        Console.WriteLine("Raw Request Body: {0}", body);

        Customer? customer = null;
        try
        {
            // Try to deserialize with Newtonsoft (project has package)
            customer = Newtonsoft.Json.JsonConvert.DeserializeObject<Customer>(body);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Deserialization failed: " + ex.Message);
        }

        if (customer == null)
        {
            // If model binding failed earlier, still attempt to bind from form data (fallback)
            try
            {
                var form = await Request.ReadFormAsync();
                var idStr = form["Id"].FirstOrDefault() ?? form["CustomerId"].FirstOrDefault();
                var name = form["Name"].FirstOrDefault() ?? form["customerName"].FirstOrDefault();
                var email = form["Email"].FirstOrDefault() ?? form["customerEmail"].FirstOrDefault();
                int.TryParse(idStr, out var idVal);
                customer = new Customer { Id = idVal, Name = name ?? string.Empty, Email = email ?? string.Empty };
                Console.WriteLine("Fallback bound from form: Name={0}, Email={1}", customer.Name, customer.Email);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fallback form bind failed: " + ex.Message);
            }
        }

        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
        {
            Console.WriteLine("Validation failed: Name = {0}, Email = {1}", customer?.Name, customer?.Email);
            return Json(new { error = "Ongeldige invoer." });
        }

        if (customer.Id > 0)
        {
            // Update existing customer
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
            // Add new customer
            _context.Customers.Add(customer);
            _context.SaveChanges();
        }

        Console.WriteLine("Customer saved successfully: Name = {0}, Email = {1}", customer.Name, customer.Email);

        return Json(new
        {
            id = customer.Id,
            name = customer.Name,
            email = customer.Email
        });
    }
}