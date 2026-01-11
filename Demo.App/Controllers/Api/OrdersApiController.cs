using Demo.App.Data;
using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class OrdersApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Get() => Ok(_db.Orders.ToList());

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var o = _db.Orders.Find(id);
        if (o == null) return NotFound();
        return Ok(o);
    }

    [HttpPost]
    public IActionResult Post([FromBody] Order order)
    {
        if (order == null) return BadRequest();
        _db.Orders.Add(order);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] Order order)
    {
        var existing = _db.Orders.Find(id);
        if (existing == null) return NotFound();
        existing.CustomerId = order.CustomerId;
        existing.BookIds = order.BookIds;
        existing.OrderDate = order.OrderDate;
        _db.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var existing = _db.Orders.Find(id);
        if (existing == null) return NotFound();
        _db.Orders.Remove(existing);
        _db.SaveChanges();
        return NoContent();
    }
}
