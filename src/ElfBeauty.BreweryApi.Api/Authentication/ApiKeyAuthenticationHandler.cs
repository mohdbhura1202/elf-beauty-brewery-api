using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ElfBeauty.BreweryApi.Api.Authentication;

/// Simple API key authentication scheme: validates the configured header against appsettings.
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private readonly ApiKeySettings _apiKeySettings;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<ApiKeySettings> apiKeySettings)
        : base(options, logger, encoder)
    {
        _apiKeySettings = apiKeySettings.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Fail closed: a blank configured key must never be treated as "any value matches".
        if (string.IsNullOrEmpty(_apiKeySettings.ApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API key authentication is misconfigured on the server."));
        }

        if (!Request.Headers.TryGetValue(_apiKeySettings.HeaderName, out var providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing '{_apiKeySettings.HeaderName}' header."));
        }

        if (string.IsNullOrEmpty(providedKey) || !IsValidKey(providedKey.ToString()))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiKeyClient") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(new
        {
            title = "Unauthorized",
            status = 401,
            detail = $"A valid '{_apiKeySettings.HeaderName}' header is required."
        });
    }

    // Constant-time comparison to avoid leaking key length/content via response-time side channels.
    private bool IsValidKey(string providedKey)
    {
        var provided = Encoding.UTF8.GetBytes(providedKey);
        var expected = Encoding.UTF8.GetBytes(_apiKeySettings.ApiKey);

        return provided.Length == expected.Length && CryptographicOperations.FixedTimeEquals(provided, expected);
    }
}
