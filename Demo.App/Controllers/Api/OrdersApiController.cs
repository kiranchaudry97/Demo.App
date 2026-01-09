using Demo.App.Data;
using Demo.SharedModels.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Demo.App.Controllers.Api;

public class CreateOrderRequest
{
    public int CustomerId { get; set; }
    public int BookId { get; set; }
}

[ApiController]
[Route("api/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersApiController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> Get()
    {
        var customers = await _context.Customers.ToListAsync();
        var books = await _context.Books.ToListAsync();

        var orders = _context.Orders
            .ToList()
            .Select(o => new
            {
                id = o.Id,
                customerName = customers.FirstOrDefault(c => c.Id == o.CustomerId)?.Name,
                books = o.BookIds.Select(bid => books.FirstOrDefault(b => b.Id == bid)?.Title).ToList(),
                orderDate = o.OrderDate.ToString("yyyy-MM-dd HH:mm:ss")
            })
            .ToList();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<ActionResult<object>> Post([FromBody] CreateOrderRequest request)
    {
        if (request == null || request.CustomerId <= 0 || request.BookId <= 0)
            return BadRequest(new { error = "Ongeldige invoer." });

        var order = new Order
        {
            CustomerId = request.CustomerId,
            BookIds = new List<int> { request.BookId },
            OrderDate = DateTime.Now
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == request.CustomerId);
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == request.BookId);

        var booksList = new List<string> { book?.Title ?? string.Empty };
        var pricesList = new List<decimal> { book?.Price ?? 0m };
        var orderTotal = pricesList.Sum();

        return Ok(new
        {
            id = order.Id,
            customerName = customer?.Name,
            books = booksList,
            prices = pricesList,
            orderTotal = orderTotal,
            orderDate = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss")
        });
    }
}
