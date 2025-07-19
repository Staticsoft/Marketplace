namespace Staticsoft.Marketplace.Abstractions;

public class Order
{
    public string Id { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal SubtotalPrice { get; set; }
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

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}
