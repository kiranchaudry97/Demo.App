namespace Demo.SharedModels.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}