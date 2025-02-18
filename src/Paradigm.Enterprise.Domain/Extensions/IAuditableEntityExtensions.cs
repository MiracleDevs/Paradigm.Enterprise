using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Extensions;

public static class IAuditableEntityExtensions
{
    public static void Audit(this IAuditableEntity entity, int? userId)
    {
        if (entity is IAuditableEntity<DateTime> dateTimeEntity)
            dateTimeEntity.Audit(userId);
        else if (entity is IAuditableEntity<DateTimeOffset> dateTimeOffsetEntity)
            dateTimeOffsetEntity.Audit(userId);
    }

    public static void Audit(this IAuditableEntity<DateTime> entity, int? userId)
    {
        var now = DateTime.UtcNow;

        if (entity.IsNew())
            entity.CreationDate = now;
        else
            entity.ModificationDate = now;

        if (userId is not null && userId != default)
        {
            if (entity.IsNew())
                entity.CreatedByUserId = userId;
            else
                entity.ModifiedByUserId = userId;
        }
    }

    public static void Audit(this IAuditableEntity<DateTimeOffset> entity, int? userId)
    {
        var now = DateTimeOffset.UtcNow;

        if (entity.IsNew())
            entity.CreationDate = now;
        else
            entity.ModificationDate = now;

        if (userId is not null && userId != default)
        {
            if (entity.IsNew())
                entity.CreatedByUserId = userId;
            else
                entity.ModifiedByUserId = userId;
        }
    }
}