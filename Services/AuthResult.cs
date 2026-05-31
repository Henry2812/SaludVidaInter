namespace SaludVidaPwa.Services;

public sealed record AuthResult(bool Success, string? ErrorMessage)
{
    public static AuthResult Ok() => new(true, null);

    public static AuthResult Fail(string errorMessage) => new(false, errorMessage);
}
