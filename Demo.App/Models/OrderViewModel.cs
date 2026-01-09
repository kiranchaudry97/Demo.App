namespace Demo.App.Models;

public class OrderViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; }
    public List<string> BookTitles { get; set; }
    public DateTime OrderDate { get; set; }
    public List<decimal> BookPrices { get; set; } = new List<decimal>();
    public decimal OrderTotal { get; set; }
}