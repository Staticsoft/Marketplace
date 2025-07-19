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
                    public class Node : ShopifyProducts.Product
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
                                public class Node : ShopifyProducts.Variant
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

    class CreateResponse
    {
        public required Data data { get; init; }
        public class Data
        {
            public required ProductCreate productCreate { get; init; }
            public class ProductCreate
            {
                public required Product product { get; init; }
                public class Product : ShopifyProducts.Product
                {
                    public required string id { get; init; }
                    public required string title { get; init; }
                    public required string descriptionHtml { get; init; }
                    public required string status { get; init; }
                    public required string createdAt { get; init; }
                    public required string updatedAt { get; init; }
                }
            }
        }
    }

    class LocationsResponse
    {
        public required Data data { get; init; }
        public class Data
        {
            public required Locations locations { get; init; }
            public class Locations
            {
                public required Edge[] edges { get; init; }
                public class Edge
                {
                    public required Node node { get; init; }
                    public class Node
                    {
                        public required string id { get; init; }
                        public required string name { get; init; }
                        public required bool isActive { get; init; }
                        public required bool deactivatable { get; init; }
                    }
                }
            }
        }
    }

    class CreateVariantsResponse
    {
        public required Data data { get; init; }
        public class Data
        {
            public required ProductVariantsBulkCreate productVariantsBulkCreate { get; init; }
            public class ProductVariantsBulkCreate
            {
                public required ProductVariant[] productVariants { get; init; }
                public class ProductVariant : Variant
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

    class GetResponse
    {
        public required Data data { get; init; }
        public class Data
        {
            public required Product product { get; init; }
            public class Product : ShopifyProducts.Product
            {
                public required string id { get; init; }
                public required string title { get; init; }
                public required string descriptionHtml { get; init; }
                public required string status { get; init; }
                public required string createdAt { get; init; }
                public required string updatedAt { get; init; }
                public required Variants variants { get; init; }
                public class Variants
                {
                    public required Edge[] edges { get; init; }
                    public class Edge
                    {
                        public required Node node { get; init; }
                        public class Node : Variant
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

    interface Product
    {
        string id { get; }
        string title { get; }
        string descriptionHtml { get; }
        string status { get; }
        string createdAt { get; }
        string updatedAt { get; }
    }

    interface Variant
    {
        string id { get; }
        string title { get; }
        string price { get; }
        int inventoryQuantity { get; }
        string createdAt { get; }
        string updatedAt { get; }
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
        var listResponse = Deserialize<ListResponse>(response.Json);

        return listResponse.data.products.edges
            .Select(edge => edge.node)
            .Select(edge => ToProduct(edge, edge.variants.edges.Select(edge => edge.node)))
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
            var getResponse = Deserialize<GetResponse>(response.Json);

            return ToProduct(
                getResponse.data.product,
                getResponse.data.product.variants.edges.Select(edge => edge.node)
            );
        }
        catch (ShopifyException ex) when (ex.Message.Contains("Not Found") || ex.Message.Contains("404"))
        {
            throw new Products.NotFoundException(productId);
        }
    }

    public async Task<Abstractions.Product> Create(NewProduct newProduct)
    {
        var product = await CreateProduct(newProduct);
        var variants = await CreateVariants(product, newProduct.Variations);
        return ToProduct(
            product.data.productCreate.product,
            variants.data.productVariantsBulkCreate.productVariants
        );
    }

    async Task<CreateResponse> CreateProduct(NewProduct newProduct)
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
            status = MapToShopifyStatus(newProduct.Status).ToUpper()
        };

        var variables = new Dictionary<string, object> { ["input"] = input };
        var request = new GraphRequest { Query = mutation, Variables = variables };
        var response = await Graph.PostAsync(request);
        return Deserialize<CreateResponse>(response.Json);
    }

    async Task<string> GetDefaultLocationId()
    {
        const string query = @"
            query {
                locations(first: 10, includeInactive: false) {
                    edges {
                        node {
                            id
                            name
                            isActive
                            deactivatable
                        }
                    }
                }
            }";

        var request = new GraphRequest { Query = query };
        var response = await Graph.PostAsync(request);
        var locationsResponse = Deserialize<LocationsResponse>(response.Json);

        var defaultLocation = locationsResponse.data.locations.edges
            .FirstOrDefault(edge => !edge.node.deactivatable)
            ?? throw new InvalidOperationException("No active locations found in the store");
        return defaultLocation.node.id;
    }

    async Task<CreateVariantsResponse> CreateVariants(CreateResponse product, NewProduct.Variation[] variations)
    {
        if (variations.Length == 0)
        {
            return new() { data = new() { productVariantsBulkCreate = new() { productVariants = [] } } };
        }

        var defaultLocationId = await GetDefaultLocationId();

        const string mutation = @"
            mutation productVariantsBulkCreate($productId: ID!, $variants: [ProductVariantsBulkInput!]!) {
                productVariantsBulkCreate(productId: $productId, variants: $variants) {
                    productVariants {
                        id
                        title
                        price
                        inventoryQuantity
                        createdAt
                        updatedAt
                    }
                    userErrors {
                        field
                        message
                    }
                }
            }";

        var variants = variations.Select(variation => new
        {
            optionValues = new[]
            {
                new
                {
                    name = variation.Title,
                    optionName = "Title"
                }
            },
            price = variation.Price,
            inventoryQuantities = new[]
            {
                new
                {
                    availableQuantity = variation.InventoryQuantity,
                    locationId = defaultLocationId
                }
            }
        }).ToArray();

        var variables = new Dictionary<string, object>
        {
            ["productId"] = product.data.productCreate.product.id,
            ["variants"] = variants
        };

        var request = new GraphRequest { Query = mutation, Variables = variables };
        var response = await Graph.PostAsync(request);
        return Deserialize<CreateVariantsResponse>(response.Json);
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

    static Abstractions.Product ToProduct(Product product, IEnumerable<Variant> variants) => new()
    {
        Id = ExtractIdFromGid(product.id),
        Title = product.title,
        Description = product.descriptionHtml,
        Status = MapProductStatus(product.status),
        CreatedAt = GetDateTimeValue(product.createdAt),
        UpdatedAt = GetDateTimeValue(product.updatedAt),
        Variations = variants.Select(ToVariation).ToArray()
    };

    static Abstractions.Product.Variation ToVariation(Variant variant)
        => new()
        {
            Id = ExtractIdFromGid(variant.id),
            Title = variant.title,
            Price = decimal.Parse(variant.price),
            InventoryQuantity = variant.inventoryQuantity,
            CreatedAt = GetDateTimeValue(variant.createdAt),
            UpdatedAt = GetDateTimeValue(variant.updatedAt)
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

    static T Deserialize<T>(IJsonElement json)
        => JsonSerializer.Deserialize<T>(json.GetRawText())
        ?? throw new Exception($"Expected '{typeof(T).Name}' got '{json.GetRawText()}'");
}

public static class IJsonElementExtensions
{
    public static IEnumerable<IJsonElement> AsArray(this IJsonElement element)
        => element.EnumerateObject();
}
