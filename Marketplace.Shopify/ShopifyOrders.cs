using ShopifySharp;
using ShopifySharp.Factories;
using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Shopify;

public class ShopifyOrders(
    IOrderServiceFactory factory,
    ShopifyOrders.Options options
) : Orders
{
    public class Options
    {
        public required string ShopDomain { get; init; }
        public required string AccessToken { get; init; }
    }

    readonly IOrderService Orders = factory.Create(new(options.ShopDomain, options.AccessToken));

    public async Task<IReadOnlyCollection<Abstractions.Order>> List()
    {
        var shopifyOrders = await Orders.ListAsync();

        return shopifyOrders.Items
            .Select(ToOrder)
            .ToList()
            .AsReadOnly();
    }

    public async Task<Abstractions.Order> Get(string orderId)
    {
        if (!long.TryParse(orderId, out var shopifyOrderId))
        {
            throw new ArgumentException($"Invalid order ID format: {orderId}", nameof(orderId));
        }

        try
        {
            var shopifyOrder = await Orders.GetAsync(shopifyOrderId);
            return ToOrder(shopifyOrder);
        }
        catch (ShopifyException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("404"))
        {
            throw new Orders.NotFoundException(orderId);
        }
    }

    public async Task<Abstractions.Order> Create(NewOrder newOrder)
    {
        var shopifyOrder = new ShopifySharp.Order
        {
            Email = newOrder.CustomerEmail,
            TotalPrice = newOrder.TotalPrice,
            SubtotalPrice = newOrder.SubtotalPrice,
            TotalTax = newOrder.TaxAmount,
            Currency = newOrder.Currency,
            FinancialStatus = MapToShopifyFinancialStatus(newOrder.Status),
            FulfillmentStatus = MapToShopifyFulfillmentStatus(newOrder.Status),
            LineItems = newOrder.Items.Select(item => new LineItem
            {
                Title = item.Title,
                Quantity = item.Quantity,
                Price = item.Price
            })
        };

        var createdOrder = await Orders.CreateAsync(shopifyOrder);
        return ToOrder(createdOrder);
    }

    public async Task Delete(string orderId)
    {
        if (!long.TryParse(orderId, out var shopifyOrderId))
        {
            throw new ArgumentException($"Invalid order ID format: {orderId}", nameof(orderId));
        }

        try
        {
            await Orders.DeleteAsync(shopifyOrderId);
        }
        catch (ShopifyException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("404"))
        {
            throw new Orders.NotFoundException(orderId);
        }
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
        CustomerEmail = shopifyOrder.Email ?? string.Empty,
        Items = shopifyOrder.LineItems
            .Select(item => new Abstractions.Order.Item
            {
                Title = item.Title,
                Quantity = item.Quantity ?? 0,
                Price = item.Price ?? 0
            })
            .ToArray()
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
