using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Forms;
using SaludVidaPwa.Models;

namespace SaludVidaPwa.Services;

public sealed class ProductService(HttpClient httpClient, IConfiguration configuration, AuthService authService) : IProductService
{
    private const string ProductsTable = "products";
    private const string ProductImagesBucket = "product-images";
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<Product>> GetProductsAsync()
    {
        var options = GetOptions();

        if (!options.IsConfigured)
        {
            return [];
        }

        using var request = CreateSupabaseRequest(HttpMethod.Get, $"{options.Url}/rest/v1/{ProductsTable}?select=*&order=created_at.desc", options);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<ProductRow>>(_jsonOptions);
        return rows?.Select(ToProduct).ToList() ?? [];
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        var products = await GetProductsAsync();
        return ["Todos", .. products.Select(product => product.Category).Where(category => !string.IsNullOrWhiteSpace(category)).Distinct()];
    }

    public async Task<IReadOnlyList<DashboardStat>> GetDashboardStatsAsync()
    {
        var products = await GetProductsAsync();
        var stockValue = products.Sum(product => product.Price * product.Stock);

        return
        [
            new("Productos Totales", products.Count.ToString()),
            new("Unidades en Stock", products.Sum(product => product.Stock).ToString()),
            new("Valor Inventario", stockValue.ToString("C"))
        ];
    }

    public async Task<Product> SaveProductAsync(ProductEditModel product, IBrowserFile? imageFile)
    {
        var options = GetOptions();
        var token = await RequireTokenAsync();

        var imageUrl = product.ImageUrl;

        if (imageFile is not null)
        {
            imageUrl = await UploadImageAsync(imageFile, options, token);
        }

        var payload = new ProductWriteRow
        {
            Name = product.Name.Trim(),
            Description = product.Description.Trim(),
            Category = product.Category.Trim(),
            Price = product.Price,
            Stock = product.Stock,
            Status = ToDatabaseStatus(product.Status),
            ImageUrl = imageUrl
        };

        var isCreate = product.Id is null or 0;
        var url = isCreate
            ? $"{options.Url}/rest/v1/{ProductsTable}"
            : $"{options.Url}/rest/v1/{ProductsTable}?id=eq.{product.Id}";

        using var request = CreateSupabaseRequest(isCreate ? HttpMethod.Post : HttpMethod.Patch, url, options, token);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent.Create(payload, options: _jsonOptions);

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<ProductRow>>(_jsonOptions);
        var saved = rows?.FirstOrDefault() ?? throw new InvalidOperationException("Supabase no regreso el producto guardado.");
        return ToProduct(saved);
    }

    public async Task DeleteProductAsync(int productId)
    {
        var options = GetOptions();
        var token = await RequireTokenAsync();

        using var request = CreateSupabaseRequest(HttpMethod.Delete, $"{options.Url}/rest/v1/{ProductsTable}?id=eq.{productId}", options, token);
        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> UploadImageAsync(IBrowserFile imageFile, SupabaseOptions options, string token)
    {
        var extension = Path.GetExtension(imageFile.Name);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var storagePath = $"products/{fileName}";
        var uploadUrl = $"{options.Url}/storage/v1/object/{ProductImagesBucket}/{storagePath}";

        await using var stream = imageFile.OpenReadStream(MaxImageBytes);
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(imageFile.ContentType);

        using var request = CreateSupabaseRequest(HttpMethod.Post, uploadUrl, options, token);
        request.Content = content;

        using var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return $"{options.Url}/storage/v1/object/public/{ProductImagesBucket}/{storagePath}";
    }

    private async Task<string> RequireTokenAsync()
    {
        var token = await authService.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Necesitas iniciar sesion como administrador.");
        }

        return token;
    }

    private HttpRequestMessage CreateSupabaseRequest(HttpMethod method, string url, SupabaseOptions options, string? bearerToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken ?? options.AnonKey);
        return request;
    }

    private SupabaseOptions GetOptions()
    {
        var url = configuration["Supabase:Url"]?.Trim().TrimEnd('/') ?? string.Empty;
        var anonKey = configuration["Supabase:AnonKey"]?.Trim() ?? string.Empty;
        return new SupabaseOptions(url, anonKey);
    }

    private static Product ToProduct(ProductRow row)
    {
        return new Product
        {
            Id = row.Id,
            Name = row.Name,
            Description = row.Description ?? string.Empty,
            Category = row.Category,
            Price = row.Price,
            Stock = row.Stock,
            Status = FromDatabaseStatus(row.Status),
            ImageUrl = row.ImageUrl ?? string.Empty
        };
    }

    private static ProductStatus FromDatabaseStatus(string status)
    {
        return status switch
        {
            "low_stock" => ProductStatus.LowStock,
            "inactive" => ProductStatus.Inactive,
            _ => ProductStatus.Active
        };
    }

    private static string ToDatabaseStatus(ProductStatus status)
    {
        return status switch
        {
            ProductStatus.LowStock => "low_stock",
            ProductStatus.Inactive => "inactive",
            _ => "active"
        };
    }

    private sealed record SupabaseOptions(string Url, string AnonKey)
    {
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey);
    }

    private sealed class ProductRow
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }
    }

    private sealed class ProductWriteRow
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("stock")]
        public int Stock { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";

        [JsonPropertyName("image_url")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
