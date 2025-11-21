using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Services;

public interface ILoggedUserService
{
    /// <summary>
    /// Authenticates the specified user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <param name="user">The user.</param>
    void Authenticate<TUser>(TUser? user) where TUser : IEntity;

    /// <summary>
    /// Tries to the get authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    TUser? TryGetAuthenticatedUser<TUser>() where TUser : IEntity;

    /// <summary>
    /// Gets the authenticated user.
    /// </summary>
    /// <typeparam name="TUser">The type of the user.</typeparam>
    /// <returns></returns>
    TUser GetAuthenticatedUser<TUser>() where TUser : IEntity;
}
