using Demo.App.Data;
using Demo.SharedModels.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.App.Controllers.Api;

[ApiController]
[Route("api/books")]
public class BooksApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public BooksApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Book>>> Get()
    {
        var books = await _context.Books.ToListAsync();
        return Ok(books);
    }
}
