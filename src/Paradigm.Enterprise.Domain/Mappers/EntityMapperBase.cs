using Mapster;
using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Domain.Mappers;

public abstract class EntityMapperBase<TInterface, TEntity, TView> : MapperBase<TEntity, TView>
    where TInterface : Interfaces.IEntity
    where TEntity : EntityBase<TInterface, TEntity, TView>, TInterface, new()
    where TView : EntityBase, TInterface, new()
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