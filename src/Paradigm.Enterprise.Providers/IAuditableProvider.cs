using Paradigm.Enterprise.Domain.Entities;

namespace Paradigm.Enterprise.Providers;

public interface IAuditableProvider<TView> : IEditProvider<TView>
    where TView : EntityBase, new()
{
}