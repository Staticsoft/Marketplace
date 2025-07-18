namespace Staticsoft.Marketplace.Abstractions;

public class NewOrder
{
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public Item[] Items { get; set; } = [];

    public class Item
    {
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
