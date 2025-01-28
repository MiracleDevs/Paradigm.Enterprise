using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Providers.Utils;

namespace Paradigm.Enterprise.Providers;

public abstract class AuditableProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository> : EditProviderBase<TInterface, TEntity, TView, TRepository, TViewRepository>, IAuditableProvider<TView>
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, IAuditableEntity, new()
    where TView : EntityBase, TInterface, new()
    where TRepository : IEditRepository<TEntity>
    where TViewRepository : IReadRepository<TView>
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditableProviderBase{TInterface, TEntity, TView, TRepository, TViewRepository}"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    protected AuditableProviderBase(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Executes before an entity is saved.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns></returns>
    protected override Task BeforeSaveAsync(TEntity entity)
    {
        var loggedUserManager = ServiceProvider.GetRequiredService<LoggedUserManager>();
        Audit(entity, loggedUserManager.TryGetAuthenticatedUser<Interfaces.IEntity>()?.Id);
        return base.BeforeSaveAsync(entity);
    }

    /// <summary>
    /// Fill the audit fields using the logged user identifier.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="userId">The user identifier.</param>
    protected virtual void Audit(TEntity entity, int? userId)
    {
        entity.Audit(userId);
    }

    #endregion
}