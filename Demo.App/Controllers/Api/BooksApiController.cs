using Demo.App.Data;
using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class BooksApiController : ControllerBase
{
    private readonly AppDbContext _db;
    public BooksApiController(AppDbContext db) => _db = db;

    [HttpGet]
    public IActionResult Get() => Ok(_db.Books.ToList());

    [HttpGet("{id}")]
    public IActionResult Get(int id)
    {
        var b = _db.Books.Find(id);
        if (b == null) return NotFound();
        return Ok(b);
    }

    [HttpPost]
    public IActionResult Post([FromBody] Book book)
    {
        if (book == null) return BadRequest();
        _db.Books.Add(book);
        _db.SaveChanges();
        return CreatedAtAction(nameof(Get), new { id = book.Id }, book);
    }

    [HttpPut("{id}")]
    public IActionResult Put(int id, [FromBody] Book book)
    {
        var existing = _db.Books.Find(id);
        if (existing == null) return NotFound();
        existing.Title = book.Title;
        existing.Author = book.Author;
        existing.Price = book.Price;
        _db.SaveChanges();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var existing = _db.Books.Find(id);
        if (existing == null) return NotFound();
        _db.Books.Remove(existing);
        _db.SaveChanges();
        return NoContent();
    }
}
