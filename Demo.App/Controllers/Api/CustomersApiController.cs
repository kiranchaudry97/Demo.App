using Demo.App.Data;
using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class CustomersApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public CustomersApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Get() => Ok(_db.Customers.ToList());

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var c = _db.Customers.Find(id);
        if (c == null) return NotFound();
        return Ok(c);
    }

    [HttpPost]
    public IActionResult Post([FromBody] Customer customer)
    {
        if (customer == null) return BadRequest();
        _db.Customers.Add(customer);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] Customer customer)
    {
        var existing = _db.Customers.Find(id);
        if (existing == null) return NotFound();
        existing.Name = customer.Name;
        existing.Email = customer.Email;
        _db.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var existing = _db.Customers.Find(id);
        if (existing == null) return NotFound();
        _db.Customers.Remove(existing);
        _db.SaveChanges();
        return NoContent();
    }
}
