using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Entities;

public static class LoggedUserManager
{
    #region Properties

    /// <summary>
    /// Gets the logged user.
    /// </summary>
    private static IEntity? User { get; set; }

    #endregion

    #region Public Methods

    /// <summary>
    /// Authenticates the specified user.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="user">The user.</param>
    public static void Authenticate<TUser>(TUser? user) where TUser : IEntity
    {
        User = user;
    }

    /// <summary>
    /// Tries to get the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    public static TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity => (TUser?)User;

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    /// <exception cref="NotAuthorizedException"></exception>
    public static TUser GetAuthenticatedUser<TUser>() where TUser : IEntity => TryGetAuthenticatedUser<TUser>() ?? throw new UnauthorizedAccessException();

    #endregion
}