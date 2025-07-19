namespace Staticsoft.Marketplace.Abstractions;

public class NewProduct
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductStatus Status { get; set; } = ProductStatus.Draft;
    public Variation[] Variations { get; set; } = [];

    public class Variation
    {
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int InventoryQuantity { get; set; }
    }
}
