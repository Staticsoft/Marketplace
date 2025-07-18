using ShopifySharp;
using ShopifySharp.Factories;
using ShopifySharp.Infrastructure.Serialization.Json;
using Staticsoft.Marketplace.Abstractions;

namespace Staticsoft.Marketplace.Shopify;

public class ShopifyProducts(
    IGraphServiceFactory factory,
    ShopifyOptions options
) : Products
{
    readonly IGraphService Graph = factory.Create(new(options.ShopDomain, options.AccessToken));

    public async Task<IReadOnlyCollection<Abstractions.Product>> List()
    {
        const string query = @"
            query {
                products(first: 250) {
                    edges {
                        node {
                            id
                            title
                            descriptionHtml
                            status
                            createdAt
                            updatedAt
                            variants(first: 250) {
                                edges {
                                    node {
                                        id
                                        title
                                        price
                                        inventoryQuantity
                                        createdAt
                                        updatedAt
                                    }
                                }
                            }
                        }
                    }
                }
            }";

        var request = new GraphRequest { Query = query };
        var response = await Graph.PostAsync(request);

        if (!response.Json.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("products", out var products) ||
            !products.TryGetProperty("edges", out var edges)
        )
        {
            return [];
        }

        var result = new List<Abstractions.Product>();
        foreach (var edge in edges.AsArray())
        {
            if (edge.TryGetProperty("node", out var node))
            {
                result.Add(ToProduct(node));
            }
        }

        return result.AsReadOnly();
    }

    public async Task<Abstractions.Product> Get(string productId)
    {
        const string query = @"
            query($id: ID!) {
                product(id: $id) {
                    id
                    title
                    descriptionHtml
                    status
                    createdAt
                    updatedAt
                    variants(first: 250) {
                        edges {
                            node {
                                id
                                title
                                price
                                inventoryQuantity
                                createdAt
                                updatedAt
                            }
                        }
                    }
                }
            }";

        var variables = new Dictionary<string, object> { ["id"] = $"gid://shopify/Product/{productId}" };
        var request = new GraphRequest { Query = query, Variables = variables };

        try
        {
            var response = await Graph.PostAsync(request);

            if (!response.Json.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("product", out var product))
            {
                throw new Products.NotFoundException(productId);
            }

            return ToProduct(product);
        }
        catch (ShopifyException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("404"))
        {
            throw new Products.NotFoundException(productId);
        }
    }

    public async Task<Abstractions.Product> Create(NewProduct newProduct)
    {
        const string mutation = @"
            mutation productCreate($input: ProductInput!) {
                productCreate(input: $input) {
                    product {
                        id
                        title
                        descriptionHtml
                        status
                        createdAt
                        updatedAt
                        variants(first: 250) {
                            edges {
                                node {
                                    id
                                    title
                                    price
                                    inventoryQuantity
                                    createdAt
                                    updatedAt
                                }
                            }
                        }
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var input = new
        {
            title = newProduct.Title,
            descriptionHtml = newProduct.Description,
            status = MapToShopifyStatus(newProduct.Status).ToUpper(),
            variants = newProduct.Variations.Select(variation => new
            {
                title = variation.Title,
                price = variation.Price.ToString("F2"),
                inventoryQuantity = variation.InventoryQuantity,
                inventoryManagement = "SHOPIFY"
            }).ToArray()
        };

        var variables = new Dictionary<string, object> { ["input"] = input };
        var request = new GraphRequest { Query = mutation, Variables = variables };
        var response = await Graph.PostAsync(request);

        if (!response.Json.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("productCreate", out var productCreate))
        {
            throw new InvalidOperationException("Failed to create product: Invalid response structure");
        }

        var hasErrors = productCreate.TryGetProperty("userErrors", out var userErrors)
            && userErrors.GetArrayLength() > 0;
        if (hasErrors)
        {
            var errorMessages = new List<string>();
            foreach (var error in userErrors.AsArray())
            {
                if (error.TryGetProperty("message", out var message))
                {
                    var messageText = message.GetRawText().Trim('"');
                    if (!string.IsNullOrEmpty(messageText))
                    {
                        errorMessages.Add(messageText);
                    }
                }
            }
            throw new InvalidOperationException($"Failed to create product: {string.Join(", ", errorMessages)}");
        }

        if (!productCreate.TryGetProperty("product", out var createdProduct))
        {
            throw new InvalidOperationException("Failed to create product: No product returned");
        }

        return ToProduct(createdProduct);
    }

    public async Task Delete(string productId)
    {
        const string mutation = @"
            mutation productDelete($input: ProductDeleteInput!) {
                productDelete(input: $input) {
                    deletedProductId
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var input = new { id = $"gid://shopify/Product/{productId}" };
        var variables = new Dictionary<string, object> { ["input"] = input };
        var request = new GraphRequest { Query = mutation, Variables = variables };

        try
        {
            var response = await Graph.PostAsync(request);

            if (!response.Json.TryGetProperty("data", out var data) ||
                !data.TryGetProperty("productDelete", out var productDelete))
            {
                throw new InvalidOperationException("Failed to delete product: Invalid response structure");
            }

            if (productDelete.TryGetProperty("userErrors", out var userErrors) && userErrors.GetArrayLength() > 0)
            {
                var errorMessages = new List<string>();
                foreach (var error in userErrors.AsArray())
                {
                    var message = GetStringValue(error, "message");
                    if (!string.IsNullOrEmpty(message))
                    {
                        errorMessages.Add(message);
                    }
                }

                var errorMessage = string.Join(", ", errorMessages);
                if (errorMessage.Contains("not found") || errorMessage.Contains("Not Found"))
                {
                    throw new Products.NotFoundException(productId);
                }

                throw new InvalidOperationException($"Failed to delete product: {errorMessage}");
            }

            if (!productDelete.TryGetProperty("deletedProductId", out var deletedProductId))
            {
                throw new Products.NotFoundException(productId);
            }
        }
        catch (ShopifyException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("404"))
        {
            throw new Products.NotFoundException(productId);
        }
    }

    static Abstractions.Product ToProduct(IJsonElement shopifyProduct) => new()
    {
        Id = ExtractIdFromGid(GetStringValue(shopifyProduct, "id")),
        Title = GetStringValue(shopifyProduct, "title"),
        Description = GetStringValue(shopifyProduct, "descriptionHtml"),
        Status = MapProductStatus(GetStringValue(shopifyProduct, "status")),
        CreatedAt = GetDateTimeValue(shopifyProduct, "createdAt"),
        UpdatedAt = GetDateTimeValue(shopifyProduct, "updatedAt"),
        Variations = GetVariations(shopifyProduct)
    };

    static string GetStringValue(IJsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetRawText().Trim('"');
        }
        return string.Empty;
    }

    static DateTime GetDateTimeValue(IJsonElement element, string propertyName)
    {
        var stringValue = GetStringValue(element, propertyName);
        return DateTime.TryParse(stringValue, out DateTime result) ? result : DateTime.MinValue;
    }

    static Abstractions.Product.Variation[] GetVariations(IJsonElement shopifyProduct)
    {
        if (!shopifyProduct.TryGetProperty("variants", out var variants) ||
            !variants.TryGetProperty("edges", out var edges))
        {
            return [];
        }

        var variations = new List<Abstractions.Product.Variation>();
        foreach (var edge in edges.AsArray())
        {
            if (edge.TryGetProperty("node", out var node))
            {
                variations.Add(new Abstractions.Product.Variation
                {
                    Id = ExtractIdFromGid(GetStringValue(node, "id")),
                    Title = GetStringValue(node, "title"),
                    Price = GetDecimalValue(node, "price"),
                    InventoryQuantity = GetIntValue(node, "inventoryQuantity"),
                    CreatedAt = GetDateTimeValue(node, "createdAt"),
                    UpdatedAt = GetDateTimeValue(node, "updatedAt")
                });
            }
        }

        return variations.ToArray();
    }

    static decimal GetDecimalValue(IJsonElement element, string propertyName)
    {
        var stringValue = GetStringValue(element, propertyName);
        return decimal.TryParse(stringValue, out decimal result) ? result : 0m;
    }

    static int GetIntValue(IJsonElement element, string propertyName)
    {
        var stringValue = GetStringValue(element, propertyName);
        return int.TryParse(stringValue, out int result) ? result : 0;
    }

    static string ExtractIdFromGid(string gid)
    {
        // Convert "gid://shopify/Product/123" to "123"
        if (string.IsNullOrEmpty(gid)) return string.Empty;
        var lastSlashIndex = gid.LastIndexOf('/');
        return lastSlashIndex >= 0 ? gid.Substring(lastSlashIndex + 1) : gid;
    }

    static ProductStatus MapProductStatus(string? status)
        => status?.ToLower() switch
        {
            "active" => ProductStatus.Active,
            "archived" => ProductStatus.Archived,
            "draft" or _ => ProductStatus.Draft
        };

    static string MapToShopifyStatus(ProductStatus status)
        => status switch
        {
            ProductStatus.Active => "active",
            ProductStatus.Archived => "archived",
            ProductStatus.Draft => "draft",
            _ => "draft"
        };
}

public static class IJsonElementExtensions
{
    public static IReadOnlyCollection<IJsonElement> AsArray(this IJsonElement element)
        => Enumerable
            .Range(0, element.GetArrayLength())
            .Select(i => element.GetProperty($"[{i}"))
            .ToArray()
            .AsReadOnly();
}