namespace Demo.SharedModels.Models;

public class OrderViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<string> BookTitles { get; set; } = new List<string>();
    public List<decimal> BookPrices { get; set; } = new List<decimal>();
    public DateTime OrderDate { get; set; }
    public decimal OrderTotal { get; set; }
}