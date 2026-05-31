using SaludVidaPwa.Models;

namespace SaludVidaPwa.Services;

public interface IAppearanceService
{
    AppearanceSettings Current { get; }
    event Action? Changed;
    void Update(AppearanceSettings settings);
}
