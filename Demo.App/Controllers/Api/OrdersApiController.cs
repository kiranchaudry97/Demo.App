using Demo.App.Data;
using Demo.App.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.App.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class OrdersApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly Demo.App.Services.IRabbitMqPublisher? _publisher;
    private readonly IConfiguration _config;
    private readonly ILogger<OrdersApiController> _logger;

    public OrdersApiController(AppDbContext db, IConfiguration config, ILogger<OrdersApiController> logger, Demo.App.Services.IRabbitMqPublisher? publisher = null)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _publisher = publisher;
    }

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
        // Publish to RabbitMQ (best-effort)
        try
        {
            if (_publisher != null)
            {
                var items = (order.BookIds?.Select(id => (object)new
                {
                    BookId = id,
                    Title = _db.Books.Find(id)?.Title ?? string.Empty,
                    Price = _db.Books.Find(id)?.Price ?? 0m,
                    Quantity = 1
                }).ToList<object>()) ?? new List<object>();

                var evt = new
                {
                    EventId = Guid.NewGuid(),
                    OrderId = order.Id,
                    CustomerId = order.CustomerId,
                    CustomerName = _db.Customers.Find(order.CustomerId)?.Name,
                    Items = items,
                    OrderDate = order.OrderDate,
                    Source = "Demo.App",
                    CorrelationId = HttpContext.TraceIdentifier,
                    Version = 1
                };

                var routing = _config["RabbitMq:RoutingKey"] ?? "orders.created";
                _publisher.PublishOrderCreated(evt, routing);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish OrderCreated event for order {OrderId}", order.Id);
        }

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
