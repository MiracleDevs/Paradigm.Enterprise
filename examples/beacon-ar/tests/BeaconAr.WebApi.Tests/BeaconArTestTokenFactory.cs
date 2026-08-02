using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BeaconAr.WebApi.Tests;

internal static class BeaconArTestTokenFactory
{
    #region Constants

    public const string Audience = "api://beacon-security-tests";
    public const string Issuer = "https://issuer.beacon.security.test";
    public const string SigningKey = "beacon-ar-security-tests-signing-key-2026";

    #endregion

    #region Public Methods

    public static string Create(
        string? scope = null,
        IReadOnlyList<string>? roles = null,
        string roleClaimType = "roles",
        string issuer = Issuer,
        string audience = Audience,
        string signingKey = SigningKey,
        bool includeSubject = true,
        bool includeName = true,
        bool includeExpiration = true,
        DateTime? notBefore = null,
        DateTime? expires = null)
    {
        List<Claim> claims = [];
        if (includeSubject)
            claims.Add(new Claim("sub", "security-test-user"));
        if (includeName)
            claims.Add(new Claim("name", "Security Test User"));
        claims.Add(new Claim("email", "security@example.test"));
        if (scope is not null)
            claims.Add(new Claim("scp", scope));
        if (roles is not null)
        {
            foreach (string role in roles)
                claims.Add(new Claim(roleClaimType, role));
        }

        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(
            issuer,
            audience,
            claims,
            notBefore ?? now.AddMinutes(-1),
            includeExpiration ? expires ?? now.AddMinutes(5) : null,
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateUnsigned()
    {
        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(
            Issuer,
            Audience,
            [new Claim("sub", "security-test-user"), new Claim("name", "Security Test User")],
            now.AddMinutes(-1),
            now.AddMinutes(5));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
