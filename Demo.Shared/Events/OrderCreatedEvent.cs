using System;
using System.Collections.Generic;

namespace Demo.Shared.Events;

public record OrderItemDto(int BookId, string Title, decimal Price, int Quantity);

public class OrderCreatedEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime OrderDate { get; set; }
    public string? Source { get; set; }
    public string? CorrelationId { get; set; }
    public int Version { get; set; } = 1;
}
