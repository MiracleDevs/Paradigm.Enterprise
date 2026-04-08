using Mapster;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Mappers;

public abstract class EntityMapperBase<TId, TInterface, TEntity, TView> : MapperBase<TEntity, TView>
    where TId : struct, IEquatable<TId>
    where TInterface : Interfaces.IEntity<TId>
    where TEntity : EntityBase<TId, TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase<TId>, TInterface, new()
{
    public virtual TEntity MapFromInterface(TEntity destination, TInterface source)
    {
        return source.Adapt(destination);
    }

    public virtual TView MapFromInterface(TView destination, TInterface source)
    {
        return source.Adapt(destination);
    }

    protected bool HasCustomConfigurationRegistered()
    {
        return HasCustomConfigurationRegistered<TInterface, TEntity>();
    }
}