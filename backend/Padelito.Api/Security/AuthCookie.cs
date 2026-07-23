namespace Padelito.Api.Security;

public static class AuthCookie
{
    public const string Name = "Padelito.Auth";

    public static CookieOptions CreateOptions(DateTime expiresAt, IWebHostEnvironment environment)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = IsSecureEnvironment(environment),
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = new DateTimeOffset(expiresAt, TimeSpan.Zero),
            IsEssential = true
        };
    }

    public static CookieOptions CreateDeleteOptions(IWebHostEnvironment environment)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = IsSecureEnvironment(environment),
            SameSite = SameSiteMode.Strict,
            Path = "/"
        };
    }

    private static bool IsSecureEnvironment(IWebHostEnvironment environment)
    {
        return !environment.IsDevelopment() && !environment.IsEnvironment("Testing");
    }
}
