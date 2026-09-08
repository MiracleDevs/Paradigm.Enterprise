using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BeaconAr.WebApi.Tests;

internal static class BeaconArTestTokenFactory
{
    #region Constants

    private const string ApplicationObjectId = "00000000-0000-0000-0000-000000000002";
    private const string ApplicationSubject = "application-subject-0002";

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
        DateTime? expires = null,
        bool includeObjectId = true,
        string? identityType = "user")
    {
        List<Claim> claims = [];
        claims.Add(new Claim("tid", "beacon-security-tests"));
        if (includeObjectId)
            claims.Add(new Claim("oid", "00000000-0000-0000-0000-000000000001"));
        if (identityType is not null)
            claims.Add(new Claim("idtyp", identityType));
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

    public static string CreateApplication(IReadOnlyList<string> roles, bool includeIdentityType = true)
    {
        List<Claim> claims =
        [
            new Claim("tid", "beacon-security-tests"),
            new Claim("oid", ApplicationObjectId),
            new Claim("sub", ApplicationSubject),
        ];
        if (includeIdentityType)
            claims.Add(new Claim("idtyp", "app"));
        foreach (string role in roles)
            claims.Add(new Claim("roles", role));

        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(
            Issuer,
            Audience,
            claims,
            now.AddMinutes(-1),
            now.AddMinutes(5),
            new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateUnsigned()
    {
        DateTime now = DateTime.UtcNow;
        JwtSecurityToken token = new(
            Issuer,
            Audience,
            [new Claim("tid", "beacon-security-tests"), new Claim("oid", "00000000-0000-0000-0000-000000000001"), new Claim("sub", "security-test-user"), new Claim("idtyp", "user"), new Claim("name", "Security Test User")],
            now.AddMinutes(-1),
            now.AddMinutes(5));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
