namespace Staticsoft.Marketplace.Abstractions;

public class NewOrder
{
    public string Number { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalPrice { get; set; }
    public decimal SubtotalPrice { get; set; }
    public decimal TaxAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
