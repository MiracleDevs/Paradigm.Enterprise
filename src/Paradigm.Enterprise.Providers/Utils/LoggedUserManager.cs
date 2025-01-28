using Paradigm.Enterprise.Interfaces;
using Paradigm.Enterprise.Providers.Exceptions;

namespace Paradigm.Enterprise.Providers.Utils;

public class LoggedUserManager
{
    #region Properties

    /// <summary>
    /// Gets the logged user.
    /// </summary>
    private IEntity? User { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Authenticates the specified user.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="user">The user.</param>
    public void Authenticate<TUser>(TUser? user) where TUser : IEntity
    {
        User = user;
    }

    /// <summary>
    /// Tries to get the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    public TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity => (TUser?)User;

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    /// <exception cref="NotAuthorizedException"></exception>
    public TUser GetAuthenticatedUser<TUser>() where TUser : IEntity => TryGetAuthenticatedUser<TUser>() ?? throw new NotAuthenticatedException();

    #endregion
}