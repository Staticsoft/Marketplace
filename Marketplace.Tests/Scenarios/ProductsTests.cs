using Staticsoft.Marketplace.Abstractions;
using Staticsoft.Testing;

namespace Staticsoft.Marketplace.Tests;

public abstract class ProductsTests : TestBase<Products>, IAsyncLifetime
{
    public const string TestProductTitle = "Test Product";
    public const string TestProductDescription = "Automated testing product";

    public async Task InitializeAsync()
    {
        var products = await SUT.List();
        foreach (var product in products)
        {
            await SUT.Delete(product.Id);
        }
    }

    public Task DisposeAsync()
        => Task.CompletedTask;

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenGettingNonExistingProduct()
    {
        await Assert.ThrowsAsync<Products.NotFoundException>(() => SUT.Get("0"));
    }

    [Fact]
    public async Task ReturnsEmptyListOfProducts()
    {
        var products = await SUT.List();

        Assert.Empty(products);
    }

    [Fact]
    public async Task ReturnsSingleProductWhenProductIsCreated()
    {
        var newProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Active,
            Variations = [new() { Title = "Default", Price = 29.99m, InventoryQuantity = 10 }]
        };
        var newVariation = newProduct.Variations.Single();

        await SUT.Create(newProduct);
        var products = await SUT.List();

        var product = Assert.Single(products);
        Assert.Equal(newProduct.Title, product.Title);
        Assert.Equal(newProduct.Description, product.Description);
        Assert.Equal(newProduct.Status, product.Status);
        Assert.NotEmpty(product.Id);
        Assert.True(product.CreatedAt > DateTime.MinValue);
        Assert.True(product.UpdatedAt > DateTime.MinValue);

        var variation = Assert.Single(product.Variations);
        Assert.Equal(newVariation.Title, variation.Title);
        Assert.Equal(newVariation.Price, variation.Price);
        Assert.Equal(newVariation.InventoryQuantity, variation.InventoryQuantity);
        Assert.NotEmpty(variation.Id);
        Assert.True(variation.CreatedAt > DateTime.MinValue);
        Assert.True(variation.UpdatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task ReturnsProductByIdWhenProductIsCreated()
    {
        var newProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Draft,
            Variations = [new() { Title = "Variant 1", Price = 19.99m, InventoryQuantity = 5 }]
        };

        var createdProduct = await SUT.Create(newProduct);
        var retrievedProduct = await SUT.Get(createdProduct.Id);

        Assert.Equal(createdProduct.Id, retrievedProduct.Id);
        Assert.Equal(createdProduct.Title, retrievedProduct.Title);
        Assert.Equal(createdProduct.Description, retrievedProduct.Description);
        Assert.Equal(createdProduct.Status, retrievedProduct.Status);
        Assert.Equal(createdProduct.CreatedAt, retrievedProduct.CreatedAt);
        Assert.Equal(createdProduct.UpdatedAt, retrievedProduct.UpdatedAt);

        var retrievedVariation = Assert.Single(retrievedProduct.Variations);
        var createdVariation = Assert.Single(createdProduct.Variations);
        Assert.Equal(createdVariation.Id, retrievedVariation.Id);
        Assert.Equal(createdVariation.Title, retrievedVariation.Title);
        Assert.Equal(createdVariation.Price, retrievedVariation.Price);
        Assert.Equal(createdVariation.InventoryQuantity, retrievedVariation.InventoryQuantity);
        Assert.Equal(createdVariation.CreatedAt, retrievedVariation.CreatedAt);
        Assert.Equal(createdVariation.UpdatedAt, retrievedVariation.UpdatedAt);
    }

    [Fact]
    public async Task ThrowsNotFoundExceptionWhenDeletingNonExistingProduct()
    {
        await Assert.ThrowsAsync<Products.NotFoundException>(() => SUT.Delete("0"));
    }

    [Fact]
    public async Task ReturnsEmptyListWhenProductIsDeleted()
    {
        var newProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Archived,
            Variations = [new() { Title = "To Delete", Price = 9.99m, InventoryQuantity = 1 }]
        };

        var createdProduct = await SUT.Create(newProduct);

        await SUT.Delete(createdProduct.Id);

        var products = await SUT.List();
        Assert.Empty(products);
    }

    [Fact]
    public async Task CreatesProductWithMultipleVariations()
    {
        var newProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Active,
            Variations = [
                new() { Title = "Small", Price = 15.99m, InventoryQuantity = 20 },
                new() { Title = "Medium", Price = 19.99m, InventoryQuantity = 15 },
                new() { Title = "Large", Price = 24.99m, InventoryQuantity = 10 }
            ]
        };

        var createdProduct = await SUT.Create(newProduct);

        Assert.Equal(3, createdProduct.Variations.Length);

        var smallVariation = createdProduct.Variations.First(v => v.Title == "Small");
        Assert.Equal(15.99m, smallVariation.Price);
        Assert.Equal(20, smallVariation.InventoryQuantity);

        var mediumVariation = createdProduct.Variations.First(v => v.Title == "Medium");
        Assert.Equal(19.99m, mediumVariation.Price);
        Assert.Equal(15, mediumVariation.InventoryQuantity);

        var largeVariation = createdProduct.Variations.First(v => v.Title == "Large");
        Assert.Equal(24.99m, largeVariation.Price);
        Assert.Equal(10, largeVariation.InventoryQuantity);
    }

    [Fact]
    public async Task CreatesProductWithDifferentStatuses()
    {
        var draftProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Draft,
            Variations = [new() { Title = "Default", Price = 10.00m, InventoryQuantity = 5 }]
        };

        var activeProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Active,
            Variations = [new() { Title = "Default", Price = 20.00m, InventoryQuantity = 10 }]
        };

        var archivedProduct = new NewProduct
        {
            Title = TestProductTitle,
            Description = TestProductDescription,
            Status = ProductStatus.Archived,
            Variations = [new() { Title = "Default", Price = 30.00m, InventoryQuantity = 0 }]
        };

        var createdDraft = await SUT.Create(draftProduct);
        var createdActive = await SUT.Create(activeProduct);
        var createdArchived = await SUT.Create(archivedProduct);

        Assert.Equal(ProductStatus.Draft, createdDraft.Status);
        Assert.Equal(ProductStatus.Active, createdActive.Status);
        Assert.Equal(ProductStatus.Archived, createdArchived.Status);

        var products = await SUT.List();
        Assert.Equal(3, products.Count);
    }
}
