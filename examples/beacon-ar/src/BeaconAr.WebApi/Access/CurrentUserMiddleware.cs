using System.Diagnostics;
using System.Security.Claims;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Providers.Access;
using BeaconAr.WebApi.Security;

namespace BeaconAr.WebApi.Access;

public sealed class CurrentUserMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;

    #endregion

    #region Constructors

    public CurrentUserMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    #endregion

    #region Public Methods

    public async Task InvokeAsync(
        HttpContext context,
        PermissionEvaluator permissions,
        ICurrentUserProvider users,
        ApplicationOperationContext operationContext,
        CurrentUserAccessor accessor)
    {
        if (context.Request.Path.StartsWithSegments("/api/v1") && context.User.Identity?.IsAuthenticated == true)
        {
            ClaimsPrincipal principal = context.User;
            AuthenticatedIdentity identity = new(
                principal.FindFirstValue("iss") ?? string.Empty,
                principal.FindFirstValue("sub") ?? string.Empty,
                principal.FindFirstValue("name") ?? principal.FindFirstValue("preferred_username") ?? string.Empty,
                principal.FindFirstValue("email"),
                permissions.Evaluate(principal));
            CurrentUserDto currentUser = await users.ResolveAsync(identity, context.RequestAborted);
            accessor.User = currentUser;
            operationContext.Initialize(currentUser.Id, Activity.Current?.Id ?? context.TraceIdentifier);
        }

        await _next(context);
    }

    #endregion
}
