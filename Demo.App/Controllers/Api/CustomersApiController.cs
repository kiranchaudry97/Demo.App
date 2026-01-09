using Demo.App.Data;
using Demo.SharedModels.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.App.Controllers.Api;

[ApiController]
[Route("api/customers")]
public class CustomersApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> Get()
    {
        var customers = await _context.Customers.ToListAsync();
        return Ok(customers);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> Post([FromBody] Customer customer)
    {
        if (customer == null || string.IsNullOrWhiteSpace(customer.Name) || string.IsNullOrWhiteSpace(customer.Email))
            return BadRequest(new { error = "Invalid input." });

        if (customer.Id > 0)
        {
            var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customer.Id);
            if (existing == null) return NotFound();
            existing.Name = customer.Name;
            existing.Email = customer.Email;
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = customer.Id }, customer);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        if (customer == null) return NotFound();
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
