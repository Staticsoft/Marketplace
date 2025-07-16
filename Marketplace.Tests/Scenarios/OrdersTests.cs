using Staticsoft.Marketplace.Abstractions;
using Staticsoft.Testing;

namespace Staticsoft.Marketplace.Tests;

public abstract class OrdersTests : TestBase<Orders>, IAsyncLifetime
{
    public const string TestOrderEmail = "automated@testing.com";

    public async Task InitializeAsync()
    {
        var orders = await SUT.List();
        foreach (var order in orders)
        {
            await SUT.Delete(order.Id);
        }
    }

    public Task DisposeAsync()
        => Task.CompletedTask;

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenGettingNonExistingOrder()
    {
        await Assert.ThrowsAsync<Orders.NotFoundException>(() => SUT.Get("0"));
    }

    [Fact]
    public async Task ReturnsEmptyListOfOrders()
    {
        var orders = await SUT.List();

        Assert.Empty(orders);
    }

    [Fact]
    public async Task ReturnsSingleOrderWhenOrderIsCreated()
    {
        var newOrder = new NewOrder
        {
            Status = OrderStatus.Pending,
            TaxAmount = 5.00m,
            Currency = "USD",
            CustomerEmail = TestOrderEmail,
            Items = [new() { Title = "Test item", Quantity = 1, Price = 10 }]
        };
        var newItem = newOrder.Items.Single();

        await SUT.Create(newOrder);
        var orders = await SUT.List();

        var order = Assert.Single(orders);
        Assert.Equal(newOrder.Status, order.Status);
        Assert.Equal(newItem.Price + newOrder.TaxAmount, order.TotalPrice);
        Assert.Equal(newItem.Price, order.SubtotalPrice);
        Assert.Equal(newOrder.TaxAmount, order.TaxAmount);
        Assert.Equal(newOrder.Currency, order.Currency);
        Assert.Equal(newOrder.CustomerEmail, order.CustomerEmail);

        var item = Assert.Single(order.Items);
        Assert.Equal(newItem.Title, item.Title);
        Assert.Equal(newItem.Quantity, item.Quantity);
        Assert.Equal(newItem.Price, item.Price);
    }

    [Fact]
    public async Task ReturnsOrderByIdWhenOrderIsCreated()
    {
        var newOrder = new NewOrder
        {
            Status = OrderStatus.Confirmed,
            TaxAmount = 5.00m,
            Currency = "USD",
            CustomerEmail = TestOrderEmail,
            Items = [new() { Title = "Test item", Quantity = 1, Price = 10 }]
        };

        var createdOrder = await SUT.Create(newOrder);
        var retrievedOrder = await SUT.Get(createdOrder.Id);

        Assert.Equal(createdOrder.Id, retrievedOrder.Id);
        Assert.NotEmpty(createdOrder.Number);
        Assert.Equal(createdOrder.Number, retrievedOrder.Number);
        Assert.Equal(createdOrder.Status, retrievedOrder.Status);
        Assert.Equal(createdOrder.TotalPrice, retrievedOrder.TotalPrice);
        Assert.Equal(createdOrder.SubtotalPrice, retrievedOrder.SubtotalPrice);
        Assert.Equal(createdOrder.TaxAmount, retrievedOrder.TaxAmount);
        Assert.Equal(createdOrder.Currency, retrievedOrder.Currency);
        Assert.Equal(createdOrder.CustomerEmail, retrievedOrder.CustomerEmail);

        var retrievedItem = Assert.Single(retrievedOrder.Items);
        var createdItem = newOrder.Items.Single();
        Assert.Equal(createdItem.Title, retrievedItem.Title);
        Assert.Equal(createdItem.Quantity, retrievedItem.Quantity);
        Assert.Equal(createdItem.Price, retrievedItem.Price);
    }

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenDeletingNonExistingOrder()
    {
        await Assert.ThrowsAsync<Orders.NotFoundException>(() => SUT.Delete("0"));
    }

    [Fact]
    public async Task ReturnsEmptyListWhenOrderIsDeleted()
    {
        var newOrder = new NewOrder
        {
            Status = OrderStatus.Processing,
            TaxAmount = 5.00m,
            Currency = "USD",
            CustomerEmail = TestOrderEmail,
            Items = [new() { Title = "Test item", Quantity = 1, Price = 10 }]
        };

        var createdOrder = await SUT.Create(newOrder);

        await SUT.Delete(createdOrder.Id);

        var orders = await SUT.List();
        Assert.Empty(orders);
    }
}
