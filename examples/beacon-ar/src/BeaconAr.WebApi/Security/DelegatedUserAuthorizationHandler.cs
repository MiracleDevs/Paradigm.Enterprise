using Microsoft.AspNetCore.Authorization;

namespace BeaconAr.WebApi.Security;

internal sealed class DelegatedUserAuthorizationHandler : AuthorizationHandler<DelegatedUserRequirement>
{
    #region Overrides

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DelegatedUserRequirement requirement)
    {
        string? identityType = context.User.FindFirst("idtyp")?.Value;
        string? objectId = context.User.FindFirst("oid")?.Value;
        string? subject = context.User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(objectId) ||
            string.IsNullOrWhiteSpace(subject))
            return Task.CompletedTask;

        if (identityType is not null)
        {
            if (!string.Equals(identityType, "user", StringComparison.Ordinal))
                return Task.CompletedTask;
        }
        else if (!context.User.FindAll("scp").Any(static claim => !string.IsNullOrWhiteSpace(claim.Value)))
        {
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    #endregion
}
