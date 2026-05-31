using Microsoft.AspNetCore.Components.Forms;
using SaludVidaPwa.Models;

namespace SaludVidaPwa.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync();
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<IReadOnlyList<DashboardStat>> GetDashboardStatsAsync();
    Task<Product> SaveProductAsync(ProductEditModel product, IBrowserFile? imageFile);
    Task DeleteProductAsync(int productId);
}
