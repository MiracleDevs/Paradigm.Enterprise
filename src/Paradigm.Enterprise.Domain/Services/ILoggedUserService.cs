using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Services;

/// <summary>
/// Stores and retrieves the authenticated entity for the current service scope.
/// </summary>
/// <typeparam name="TId">The value type used for user identifiers.</typeparam>
/// <remarks>
/// Register the service with <c>RegisterLoggedUserService</c>. Authenticating
/// <see langword="null"/> clears the current user.
/// </remarks>
public interface ILoggedUserService<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Sets the authenticated user for the current scope.
    /// </summary>
    /// <typeparam name="TUser">The entity type representing a user.</typeparam>
    /// <param name="user">The authenticated user, or <see langword="null"/> to clear authentication.</param>
    void Authenticate<TUser>(TUser? user) where TUser : IEntity<TId>;

    /// <summary>
    /// Attempts to get the authenticated user as the requested entity type.
    /// </summary>
    /// <typeparam name="TUser">The expected entity type for the authenticated user.</typeparam>
    /// <returns>The current user, or <see langword="null"/> when no user has been authenticated.</returns>
    /// <exception cref="InvalidCastException">The stored user is not assignable to <typeparamref name="TUser"/>.</exception>
    TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity<TId>;

    /// <summary>
    /// Gets the authenticated user as the requested entity type.
    /// </summary>
    /// <typeparam name="TUser">The expected entity type for the authenticated user.</typeparam>
    /// <returns>The current authenticated user.</returns>
    /// <exception cref="InvalidCastException">The stored user is not assignable to <typeparamref name="TUser"/>.</exception>
    /// <exception cref="UnauthorizedAccessException">No user has been authenticated.</exception>
    TUser GetAuthenticatedUser<TUser>() where TUser : IEntity<TId>;
}
