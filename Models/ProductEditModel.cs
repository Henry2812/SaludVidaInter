namespace SaludVidaPwa.Models;

public sealed class ProductEditModel
{
    public int? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Vitaminas";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public string ImageUrl { get; set; } = string.Empty;
}
