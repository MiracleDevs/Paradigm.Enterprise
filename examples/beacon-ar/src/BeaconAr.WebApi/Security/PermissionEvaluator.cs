using System.Security.Claims;

namespace BeaconAr.WebApi.Security;

public sealed class PermissionEvaluator
{
    #region Fields

    private readonly string _readPermission;
    private readonly string _writePermission;

    #endregion

    #region Constructors

    public PermissionEvaluator(IConfiguration configuration)
    {
        _readPermission = configuration["Authentication:Permissions:Read"] ?? BeaconPolicies.Read;
        _writePermission = configuration["Authentication:Permissions:Write"] ?? BeaconPolicies.Write;
    }

    #endregion

    #region Public Methods

    public IReadOnlyList<string> Evaluate(ClaimsPrincipal principal)
    {
        HashSet<string> granted = new(StringComparer.Ordinal);
        foreach (string value in principal.FindAll("scp").SelectMany(static claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            granted.Add(value);
        foreach (Claim claim in principal.FindAll("roles"))
            granted.Add(claim.Value);

        List<string> policies = [];
        if (granted.Contains(_readPermission) || granted.Contains(_writePermission))
            policies.Add(BeaconPolicies.Read);
        if (granted.Contains(_writePermission))
            policies.Add(BeaconPolicies.Write);
        return policies;
    }

    public bool HasPolicy(ClaimsPrincipal principal, string policy) => Evaluate(principal).Contains(policy, StringComparer.Ordinal);

    #endregion
}
