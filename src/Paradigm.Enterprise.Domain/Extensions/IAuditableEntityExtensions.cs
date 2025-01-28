using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Extensions;

public static class IAuditableEntityExtensions
{
    public static void Audit(this IAuditableEntity entity, int? userId)
    {
        var now = DateTimeOffset.UtcNow;

        if (entity.IsNew())
            entity.CreationDate = now;

        entity.ModificationDate = now;

        if (userId is not null && userId != default)
        {
            if (entity.IsNew())
                entity.CreatedByUserId = userId;

            entity.ModifiedByUserId = userId;
        }
    }
}