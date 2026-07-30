using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Domain.Extensions;

/// <summary>
/// Applies creation or modification audit values to supported auditable entities.
/// </summary>
/// <remarks>
/// Audit classification uses <see cref="IEntity.IsNew"/> at the moment the extension is called. Apply
/// auditing before a repository or database assigns a new identifier; otherwise a newly inserted entity
/// may be stamped as modified. The overload accepting <see cref="IAuditableEntity{TId}"/> dispatches
/// only to the built-in <see cref="DateTime"/> and <see cref="DateTimeOffset"/> timestamp contracts.
/// </remarks>
/// <example>
/// Call <c>Audit</c> from a provider's pre-staging entity hook:
/// <code>
/// public sealed class Order : EntityBase&lt;Guid&gt;, IAuditableEntity&lt;DateTimeOffset, Guid&gt;
/// {
///     public Guid? CreatedByUserId { get; set; }
///     public Guid? ModifiedByUserId { get; set; }
///     public DateTimeOffset CreationDate { get; set; }
///     public DateTimeOffset? ModificationDate { get; set; }
/// }
///
/// protected override Task BeforeSaveAsync(Order entity)
/// {
///     Guid? userId = loggedUsers.TryGetAuthenticatedUser&lt;User&gt;()?.Id;
///     entity.Audit(userId);
///     return Task.CompletedTask;
/// }
///
/// // A new order receives CreationDate and CreatedByUserId.
/// // Once Id is assigned, the same call updates ModificationDate and ModifiedByUserId.
/// </code>
/// Passing a null or default user identifier still applies the timestamp but leaves the user fields
/// unchanged.
/// </example>
public static class IAuditableEntityExtensions
{
    /// <summary>
    /// Audits an entity when its timestamp type is <see cref="DateTime"/> or <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <typeparam name="TId">The value type used for entity and user identifiers.</typeparam>
    /// <param name="entity">The entity to audit.</param>
    /// <param name="userId">
    /// The acting user's identifier. A <see langword="null"/> or default identifier leaves user fields unchanged.
    /// </param>
    /// <remarks>An implementation with another timestamp type is left unchanged.</remarks>
    public static void Audit<TId>(this IAuditableEntity<TId> entity, TId? userId)
        where TId : struct, IEquatable<TId>
    {
        if (entity is IAuditableEntity<DateTime, TId> dateTimeEntity)
            dateTimeEntity.Audit(userId);
        else if (entity is IAuditableEntity<DateTimeOffset, TId> dateTimeOffsetEntity)
            dateTimeOffsetEntity.Audit(userId);
    }

    /// <summary>
    /// Applies the current UTC <see cref="DateTime"/> and, when supplied, the acting user's identifier.
    /// </summary>
    /// <typeparam name="TId">The value type used for entity and user identifiers.</typeparam>
    /// <param name="entity">The entity to audit.</param>
    /// <param name="userId">
    /// The acting user's identifier. A <see langword="null"/> or default identifier leaves user fields unchanged.
    /// </param>
    /// <remarks>
    /// New entities receive creation values; existing entities receive modification values.
    /// Existing audit values in the other category are not cleared.
    /// </remarks>
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

    /// <summary>
    /// Applies the current UTC <see cref="DateTimeOffset"/> and, when supplied, the acting user's identifier.
    /// </summary>
    /// <typeparam name="TId">The value type used for entity and user identifiers.</typeparam>
    /// <param name="entity">The entity to audit.</param>
    /// <param name="userId">
    /// The acting user's identifier. A <see langword="null"/> or default identifier leaves user fields unchanged.
    /// </param>
    /// <remarks>
    /// New entities receive creation values; existing entities receive modification values.
    /// Existing audit values in the other category are not cleared.
    /// </remarks>
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
