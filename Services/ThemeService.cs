using Microsoft.JSInterop;

namespace SaludVidaPwa.Services;

public sealed class ThemeService(IJSRuntime jsRuntime)
{
    public async Task ApplyAsync(string primaryColor)
    {
        if (string.IsNullOrWhiteSpace(primaryColor))
        {
            return;
        }

        await jsRuntime.InvokeVoidAsync("saludVidaTheme.apply", primaryColor);
    }
}
