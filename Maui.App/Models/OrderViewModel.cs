namespace Maui.App.Models;

public class OrderViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<string> BookTitles { get; set; } = new();
    public DateTime OrderDate { get; set; }
    public List<decimal> BookPrices { get; set; } = new();
    public decimal OrderTotal { get; set; }

    public string BookTitlesString => string.Join(", ", BookTitles ?? new List<string>());

    public string BooksWithPricesString
    {
        get
        {
            if (BookTitles == null || BookTitles.Count == 0) return string.Empty;
            var parts = new List<string>();
            for (int i = 0; i < BookTitles.Count; i++)
            {
                var title = BookTitles[i] ?? string.Empty;
                var price = (BookPrices != null && BookPrices.Count > i) ? BookPrices[i] : 0m;
                parts.Add($"{title} - €{price:N2}");
            }
            return string.Join("\n", parts);
        }
    }
}