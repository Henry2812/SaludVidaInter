using SaludVidaPwa.Models;

namespace SaludVidaPwa.Services;

public sealed class AppearanceService : IAppearanceService
{
    public AppearanceSettings Current { get; private set; } = new();

    public event Action? Changed;

    public void Update(AppearanceSettings settings)
    {
        Current = settings;
        Changed?.Invoke();
    }
}
