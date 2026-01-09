using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers;

public class CustomerController : Controller
{
    private static readonly List<Customer> Customers = new()
    {
        new Customer { Id = 1, Name = "Jan Jansen", Email = "jan.jansen@example.com" },
        new Customer { Id = 2, Name = "Piet Pietersen", Email = "piet.pietersen@example.com" }
    };

    public IActionResult Index()
    {
        return View(Customers);
    }

    public IActionResult Add()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Add(Customer customer)
    {
        customer.Id = Customers.Max(c => c.Id) + 1;
        Customers.Add(customer);
        return RedirectToAction("Index");
    }

    public IActionResult Edit(int id)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == id);
        if (customer == null) return NotFound();
        return View(customer);
    }

    [HttpPost]
    public IActionResult Edit(Customer customer)
    {
        var existing = Customers.FirstOrDefault(c => c.Id == customer.Id);
        if (existing == null) return NotFound();
        existing.Name = customer.Name;
        existing.Email = customer.Email;
        return RedirectToAction("Index");
    }

    public IActionResult Delete(int id)
    {
        var customer = Customers.FirstOrDefault(c => c.Id == id);
        if (customer != null) Customers.Remove(customer);
        return RedirectToAction("Index");
    }
}