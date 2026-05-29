using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EngineeringDigest.Admin.Security;

public sealed class HeaderRoleAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EngineeringDigestHeader";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userName = Request.Headers.TryGetValue("X-User-Name", out var userHeader) ? userHeader.ToString() : "system";
        var rolesValue = Request.Headers.TryGetValue("X-User-Roles", out var rolesHeader)
            ? rolesHeader.ToString()
            : configuration["Security:DefaultRoles"] ?? "Administrator";

        var claims = new List<Claim> { new(ClaimTypes.Name, userName) };
        claims.AddRange(rolesValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(role => new Claim(ClaimTypes.Role, role)));
        var identity = new ClaimsIdentity(claims, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
