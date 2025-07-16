using Staticsoft.Marketplace.Abstractions;
using Staticsoft.Testing;

namespace Staticsoft.Marketplace.Tests;

public abstract class OrdersTests : TestBase<Orders>, IAsyncLifetime
{
    const string TestOrderPrefix = "AutomatedTestOrder";

    public async Task InitializeAsync()
    {
        var orders = await SUT.List();
        var testOrders = orders.Where(order => order.Number.StartsWith(TestOrderPrefix));
        foreach (var order in testOrders)
        {
            await SUT.Delete(order.Id);
        }
    }

    public Task DisposeAsync()
        => Task.CompletedTask;

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenGettingNonExistingOrder()
    {
        await Assert.ThrowsAsync<Orders.NotFoundException>(() => SUT.Get("-1"));
    }

    [Fact]
    public async Task ReturnsEmptyListOfOrders()
    {
        var orders = await SUT.List();
        var testOrders = orders.Where(order => order.Number.StartsWith(TestOrderPrefix));

        Assert.Empty(testOrders);
    }

    [Fact]
    public async Task ReturnsSingleOrderWhenOrderIsCreated()
    {
        var newOrder = new NewOrder
        {
            Number = $"{TestOrderPrefix}-001",
            Status = OrderStatus.Pending,
            TotalPrice = 100.00m,
            SubtotalPrice = 90.00m,
            TaxAmount = 10.00m,
            Currency = "USD",
            CustomerEmail = "customer@example.com"
        };

        await SUT.Create(newOrder);
        var orders = await SUT.List();
        var testOrders = orders.Where(order => order.Number.StartsWith(TestOrderPrefix));

        var order = Assert.Single(testOrders);
        Assert.Equal(newOrder.Number, order.Number);
        Assert.Equal(newOrder.Status, order.Status);
        Assert.Equal(newOrder.TotalPrice, order.TotalPrice);
        Assert.Equal(newOrder.SubtotalPrice, order.SubtotalPrice);
        Assert.Equal(newOrder.TaxAmount, order.TaxAmount);
        Assert.Equal(newOrder.Currency, order.Currency);
        Assert.Equal(newOrder.CustomerEmail, order.CustomerEmail);
    }

    [Fact]
    public async Task ReturnsOrderByIdWhenOrderIsCreated()
    {
        var newOrder = new NewOrder
        {
            Number = $"{TestOrderPrefix}-002",
            Status = OrderStatus.Confirmed,
            TotalPrice = 250.00m,
            SubtotalPrice = 225.00m,
            TaxAmount = 25.00m,
            Currency = "USD",
            CustomerEmail = "test@example.com"
        };

        var createdOrder = await SUT.Create(newOrder);
        var retrievedOrder = await SUT.Get(createdOrder.Id);

        Assert.Equal(createdOrder.Id, retrievedOrder.Id);
        Assert.Equal(createdOrder.Number, retrievedOrder.Number);
        Assert.Equal(createdOrder.Status, retrievedOrder.Status);
        Assert.Equal(createdOrder.TotalPrice, retrievedOrder.TotalPrice);
        Assert.Equal(createdOrder.SubtotalPrice, retrievedOrder.SubtotalPrice);
        Assert.Equal(createdOrder.TaxAmount, retrievedOrder.TaxAmount);
        Assert.Equal(createdOrder.Currency, retrievedOrder.Currency);
        Assert.Equal(createdOrder.CustomerEmail, retrievedOrder.CustomerEmail);
    }

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenDeletingNonExistingOrder()
    {
        await Assert.ThrowsAsync<Orders.NotFoundException>(() => SUT.Delete("-1"));
    }

    [Fact]
    public async Task ReturnsEmptyListWhenOrderIsDeleted()
    {
        var newOrder = new NewOrder
        {
            Number = $"{TestOrderPrefix}-003",
            Status = OrderStatus.Processing,
            TotalPrice = 75.00m,
            SubtotalPrice = 70.00m,
            TaxAmount = 5.00m,
            Currency = "USD",
            CustomerEmail = "delete@example.com"
        };

        var createdOrder = await SUT.Create(newOrder);

        await SUT.Delete(createdOrder.Id);

        var orders = await SUT.List();
        var testOrders = orders.Where(order => order.Number.StartsWith(TestOrderPrefix));
        Assert.Empty(testOrders);
    }
}
