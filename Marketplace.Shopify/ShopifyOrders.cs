using ShopifySharp;
using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Shopify;

public class ShopifyOrders(
    OrderService orders
) : Orders
{
    readonly OrderService Orders = orders;

    public async Task<IReadOnlyCollection<Abstractions.Order>> List()
    {
        var shopifyOrders = await Orders.ListAsync();

        return shopifyOrders.Items
            .Select(ToOrder)
            .ToList()
            .AsReadOnly();
    }

    static Abstractions.Order ToOrder(ShopifySharp.Order shopifyOrder) => new()
    {
        Id = shopifyOrder.Id?.ToString() ?? string.Empty,
        Number = shopifyOrder.OrderNumber?.ToString() ?? shopifyOrder.Name ?? string.Empty,
        CreatedAt = shopifyOrder.CreatedAt?.DateTime ?? DateTime.MinValue,
        UpdatedAt = shopifyOrder.UpdatedAt?.DateTime ?? DateTime.MinValue,
        Status = MapOrderStatus(shopifyOrder.FinancialStatus, shopifyOrder.FulfillmentStatus),
        TotalPrice = shopifyOrder.TotalPrice ?? 0m,
        SubtotalPrice = shopifyOrder.SubtotalPrice ?? 0m,
        TaxAmount = shopifyOrder.TotalTax ?? 0m,
        Currency = shopifyOrder.Currency ?? string.Empty,
        CustomerEmail = shopifyOrder.Email ?? string.Empty
    };

    private static OrderStatus MapOrderStatus(string? financialStatus, string? fulfillmentStatus)
    {
        return (financialStatus?.ToLower(), fulfillmentStatus?.ToLower()) switch
        {
            ("pending", _) => OrderStatus.Pending,
            ("authorized", null) or ("authorized", "unfulfilled") => OrderStatus.Confirmed,
            ("paid", "unfulfilled") or ("paid", "partial") => OrderStatus.Processing,
            ("paid", "fulfilled") => OrderStatus.Delivered,
            ("refunded", _) or ("partially_refunded", _) => OrderStatus.Refunded,
            ("voided", _) => OrderStatus.Cancelled,
            (_, "shipped") => OrderStatus.Shipped,
            _ => OrderStatus.Pending
        };
    }
}
