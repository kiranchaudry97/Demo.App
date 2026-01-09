using System.ComponentModel.DataAnnotations;

namespace Demo.App.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public List<int> BookIds { get; set; } = new();
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
}