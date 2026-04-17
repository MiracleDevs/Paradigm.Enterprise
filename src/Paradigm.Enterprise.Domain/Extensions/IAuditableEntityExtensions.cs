using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Extensions;

public static class IAuditableEntityExtensions
{
    public static void Audit<TId>(this IAuditableEntity<TId> entity, TId? userId)
        where TId : struct, IEquatable<TId>
    {
        if (entity is IAuditableEntity<DateTime, TId> dateTimeEntity)
            dateTimeEntity.Audit(userId);
        else if (entity is IAuditableEntity<DateTimeOffset, TId> dateTimeOffsetEntity)
            dateTimeOffsetEntity.Audit(userId);
    }

    public static void Audit<TId>(this IAuditableEntity<DateTime, TId> entity, TId? userId)
        where TId : struct, IEquatable<TId>
    {
        var now = DateTime.UtcNow;

        if (entity.IsNew())
            entity.CreationDate = now;
        else
            entity.ModificationDate = now;

        if (userId is not null && !userId.Value.Equals(default(TId)))
        {
            if (entity.IsNew())
                entity.CreatedByUserId = userId;
            else
                entity.ModifiedByUserId = userId;
        }
    }

    public static void Audit<TId>(this IAuditableEntity<DateTimeOffset, TId> entity, TId? userId)
        where TId : struct, IEquatable<TId>
    {
        var now = DateTimeOffset.UtcNow;

        if (entity.IsNew())
            entity.CreationDate = now;
        else
            entity.ModificationDate = now;

        if (userId is not null && !userId.Value.Equals(default(TId)))
        {
            if (entity.IsNew())
                entity.CreatedByUserId = userId;
            else
                entity.ModifiedByUserId = userId;
        }
    }
}