using SaludVidaPwa.Models;

namespace SaludVidaPwa.Services;

public sealed class CartService
{
    private readonly List<Product> _items = [];

    public event Action? Changed;

    public IReadOnlyList<Product> Items => _items;

    public int Count => _items.Count;

    public void Add(Product product)
    {
        _items.Add(product);
        Changed?.Invoke();
    }

    public void Clear()
    {
        _items.Clear();
        Changed?.Invoke();
    }
}
