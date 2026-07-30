using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Services;

internal class LoggedUserService<TId> : ILoggedUserService<TId>
    where TId : struct, IEquatable<TId>
{
    #region Properties

    /// <summary>
    /// Gets the logged user.
    /// </summary>
    private IEntity<TId>? User { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Authenticates the specified user.
    /// </summary>
    /// <typeparam name="TUser">The entity type representing a user.</typeparam>
    /// <param name="user">The authenticated user, or <see langword="null"/> to clear authentication.</param>
    public void Authenticate<TUser>(TUser? user) where TUser : IEntity<TId>
    {
        User = user;
    }

    /// <summary>
    /// Tries to get the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns>The current user, or <see langword="null"/> when no user has been authenticated.</returns>
    /// <exception cref="InvalidCastException">The stored user is not assignable to <typeparamref name="TUser"/>.</exception>
    public TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity<TId> => (TUser?)User;

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns>The current authenticated user.</returns>
    /// <exception cref="InvalidCastException">The stored user is not assignable to <typeparamref name="TUser"/>.</exception>
    /// <exception cref="UnauthorizedAccessException">No user has been authenticated.</exception>
    public TUser GetAuthenticatedUser<TUser>() where TUser : IEntity<TId> => TryGetAuthenticatedUser<TUser>() ?? throw new UnauthorizedAccessException();

    #endregion
}
