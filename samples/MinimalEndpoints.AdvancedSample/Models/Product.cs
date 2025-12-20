namespace MinimalEndpoints.AdvancedSample.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock
);

public record UpdateProductRequest(
    string? Name,
    string? Description,
    decimal? Price,
    int? Stock
);

