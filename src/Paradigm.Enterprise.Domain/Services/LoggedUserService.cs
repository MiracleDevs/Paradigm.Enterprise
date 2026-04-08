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
    /// <param name="version">The version.</param>
    /// <param name="user">The user.</param>
    public void Authenticate<TUser>(TUser? user) where TUser : IEntity<TId>
    {
        User = user;
    }

    /// <summary>
    /// Tries to get the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    public TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity<TId> => (TUser?)User;

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    /// <exception cref="NotAuthorizedException"></exception>
    public TUser GetAuthenticatedUser<TUser>() where TUser : IEntity<TId> => TryGetAuthenticatedUser<TUser>() ?? throw new UnauthorizedAccessException();

    #endregion
}