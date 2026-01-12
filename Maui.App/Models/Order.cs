namespace Maui.App.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public DateTime OrderDate { get; set; }
    // Match Demo.App Order schema which contains a Name column
    public string Name { get; set; } = string.Empty;
}
