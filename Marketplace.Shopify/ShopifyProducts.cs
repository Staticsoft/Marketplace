using ShopifySharp;
using ShopifySharp.Factories;
using ShopifySharp.Infrastructure.Serialization.Json;
using Staticsoft.Marketplace.Abstractions;
using System.Text.Json;

namespace Staticsoft.Marketplace.Shopify;

public class ShopifyProducts(
    IGraphServiceFactory factory,
    ShopifyOptions options
) : Products
{
    readonly IGraphService Graph = factory.Create(new(options.ShopDomain, options.AccessToken));

    #region Responses
    class ListResponse
    {
        public required Data data { get; init; }

        public class Data
        {
            public required Products products { get; init; }
            public class Products
            {
                public required Edge[] edges { get; init; }
                public class Edge
                {
                    public required Node node { get; init; }
                    public class Node
                    {
                        public required string id { get; init; }
                        public required string title { get; init; }
                        public required string descriptionHtml { get; init; }
                        public required string status { get; init; }
                        public required string createdAt { get; init; }
                        public required string updatedAt { get; init; }
                        public required Variant variants { get; init; }
                        public class Variant
                        {
                            public required Edge[] edges { get; init; }
                            public class Edge
                            {
                                public required Node node { get; init; }
                                public class Node
                                {
                                    public required string id { get; init; }
                                    public required string title { get; init; }
                                    public required string price { get; init; }
                                    public required int inventoryQuantity { get; init; }
                                    public required string createdAt { get; init; }
                                    public required string updatedAt { get; init; }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    #endregion

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
        var listResponse = JsonSerializer.Deserialize<ListResponse>(response.Json.GetRawText());
        if (listResponse == null) return [];

        return listResponse.data.products.edges
            .Select(edge => edge.node)
            .Select(ToProduct)
            .ToArray()
            .AsReadOnly();
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

            //return ToProduct(product);
            return null;
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
                inventoryQuantity = variation.InventoryQuantity
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

        //return ToProduct(createdProduct);
        return null;
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

    static Abstractions.Product ToProduct(ListResponse.Data.Products.Edge.Node node) => new()
    {
        Id = ExtractIdFromGid(node.id),
        Title = node.title,
        Description = node.descriptionHtml,
        Status = MapProductStatus(node.status),
        CreatedAt = GetDateTimeValue(node.createdAt),
        UpdatedAt = GetDateTimeValue(node.updatedAt),
        Variations = node.variants.edges
            .Select(edge => edge.node)
            .Select(ToVariation)
            .ToArray()
    };

    static Abstractions.Product.Variation ToVariation(
        ListResponse.Data.Products.Edge.Node.Variant.Edge.Node node
    )
        => new()
        {
            Id = ExtractIdFromGid(node.id),
            Title = node.title,
            Price = decimal.Parse(node.price),
            InventoryQuantity = node.inventoryQuantity,
            CreatedAt = GetDateTimeValue(node.createdAt),
            UpdatedAt = GetDateTimeValue(node.updatedAt)
        };

    static string GetStringValue(IJsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            return property.GetRawText().Trim('"');
        }
        return string.Empty;
    }

    static DateTime GetDateTimeValue(string dateTime)
        => DateTime.TryParse(dateTime, out DateTime result) ? result : DateTime.MinValue;

    static string ExtractIdFromGid(string gid)
    {
        // Convert "gid://shopify/Product/123" to "123"
        if (string.IsNullOrEmpty(gid)) return string.Empty;
        var lastSlashIndex = gid.LastIndexOf('/');
        return lastSlashIndex >= 0 ? gid.Substring(lastSlashIndex + 1) : gid;
    }

    static ProductStatus MapProductStatus(string status)
        => status.ToLower() switch
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
    public static IEnumerable<IJsonElement> AsArray(this IJsonElement element)
        => element.EnumerateObject();
}