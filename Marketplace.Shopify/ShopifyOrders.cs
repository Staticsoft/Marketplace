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

    public async Task<Abstractions.Order> Create(NewOrder newOrder)
    {
        var shopifyOrder = new ShopifySharp.Order
        {
            Name = newOrder.Number,
            Email = newOrder.CustomerEmail,
            TotalPrice = newOrder.TotalPrice,
            SubtotalPrice = newOrder.SubtotalPrice,
            TotalTax = newOrder.TaxAmount,
            Currency = newOrder.Currency,
            FinancialStatus = MapToShopifyFinancialStatus(newOrder.Status),
            FulfillmentStatus = MapToShopifyFulfillmentStatus(newOrder.Status)
        };

        var createdOrder = await Orders.CreateAsync(shopifyOrder);
        return ToOrder(createdOrder);
    }

    public async Task<Abstractions.Order> Get(string orderId)
    {
        if (!long.TryParse(orderId, out var shopifyOrderId))
        {
            throw new ArgumentException($"Invalid order ID format: {orderId}", nameof(orderId));
        }

        var shopifyOrder = await Orders.GetAsync(shopifyOrderId);
        return ToOrder(shopifyOrder);
    }

    public async Task Delete(string orderId)
    {
        if (!long.TryParse(orderId, out var shopifyOrderId))
        {
            throw new ArgumentException($"Invalid order ID format: {orderId}", nameof(orderId));
        }

        await Orders.DeleteAsync(shopifyOrderId);
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

    static OrderStatus MapOrderStatus(string? financialStatus, string? fulfillmentStatus)
        => (financialStatus?.ToLower(), fulfillmentStatus?.ToLower()) switch
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

    static string MapToShopifyFinancialStatus(OrderStatus status)
        => status switch
        {
            OrderStatus.Pending => "pending",
            OrderStatus.Confirmed => "authorized",
            OrderStatus.Processing => "paid",
            OrderStatus.Shipped => "paid",
            OrderStatus.Delivered => "paid",
            OrderStatus.Cancelled => "voided",
            OrderStatus.Refunded => "refunded",
            _ => "pending"
        };

    static string? MapToShopifyFulfillmentStatus(OrderStatus status)
        => status switch
        {
            OrderStatus.Pending => "unfulfilled",
            OrderStatus.Confirmed => "unfulfilled",
            OrderStatus.Processing => "unfulfilled",
            OrderStatus.Shipped => "shipped",
            OrderStatus.Delivered => "fulfilled",
            OrderStatus.Cancelled => null,
            OrderStatus.Refunded => null,
            _ => "unfulfilled"
        };
}
