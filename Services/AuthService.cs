using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;

namespace SaludVidaPwa.Services;

public sealed class AuthService(HttpClient httpClient, IConfiguration configuration, IJSRuntime jsRuntime)
{
    private const string SessionStorageKey = "saludvida.supabase.session";

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public event Action? Changed;

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var options = GetOptions();

        if (!options.IsConfigured)
        {
            return AuthResult.Fail("Falta configurar SupabaseUrl y SupabaseAnonKey en wwwroot/appsettings.json.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.Url}/auth/v1/token?grant_type=password");

        request.Headers.Add("apikey", options.AnonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.AnonKey);
        request.Content = JsonContent.Create(new { email = email.Trim(), password }, options: _jsonOptions);

        using var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return AuthResult.Fail("Correo o contrasena incorrectos en Supabase.");
        }

        var session = await response.Content.ReadFromJsonAsync<SupabaseSession>(_jsonOptions);

        if (session is null || string.IsNullOrWhiteSpace(session.AccessToken))
        {
            return AuthResult.Fail("Supabase no regreso una sesion valida.");
        }

        await SaveSessionAsync(session);
        Changed?.Invoke();
        return AuthResult.Ok();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var session = await GetSessionAsync();
        return session is not null && !string.IsNullOrWhiteSpace(session.AccessToken);
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var session = await GetSessionAsync();
        return session?.AccessToken;
    }

    public async Task LogoutAsync()
    {
        var options = GetOptions();
        var token = await GetAccessTokenAsync();

        if (options.IsConfigured && !string.IsNullOrWhiteSpace(token))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{options.Url}/auth/v1/logout");
            request.Headers.Add("apikey", options.AnonKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                await httpClient.SendAsync(request);
            }
            catch
            {
                // Si la red falla, aun limpiamos la sesion local.
            }
        }

        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", SessionStorageKey);
        Changed?.Invoke();
    }

    private SupabaseOptions GetOptions()
    {
        var url = configuration["Supabase:Url"]?.Trim().TrimEnd('/') ?? string.Empty;
        var anonKey = configuration["Supabase:AnonKey"]?.Trim() ?? string.Empty;
        return new SupabaseOptions(url, anonKey);
    }

    private async Task SaveSessionAsync(SupabaseSession session)
    {
        var json = JsonSerializer.Serialize(session, _jsonOptions);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", SessionStorageKey, json);
    }

    private async Task<SupabaseSession?> GetSessionAsync()
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", SessionStorageKey);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SupabaseSession>(json, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private sealed record SupabaseOptions(string Url, string AnonKey)
    {
        public bool IsConfigured => !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(AnonKey);
    }

    private sealed class SupabaseSession
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("user")]
        public SupabaseUser? User { get; set; }
    }

    private sealed class SupabaseUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;
    }
}
