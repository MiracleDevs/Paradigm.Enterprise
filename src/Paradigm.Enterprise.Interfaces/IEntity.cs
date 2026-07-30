using System;

namespace Paradigm.Enterprise.Interfaces;

/// <summary>
/// Defines an object whose persistence state can be classified as new or existing.
/// </summary>
/// <remarks>
/// Providers use this classification to choose between add and update paths. Implementations must keep
/// the result consistent with the identifier convention used by their persistence layer.
/// </remarks>
public interface IEntity
{
    /// <summary>
    /// Determines whether the entity has not yet been assigned a persistent identity.
    /// </summary>
    /// <returns><see langword="true"/> when the entity is new; otherwise, <see langword="false"/>.</returns>
    bool IsNew();
}

/// <summary>
/// Defines an entity with a strongly typed persistent identifier.
/// </summary>
/// <typeparam name="TId">The value type used for the entity identifier.</typeparam>
/// <example>
/// <code>
/// public sealed class Customer : IEntity&lt;Guid&gt;
/// {
///     public Guid Id { get; private set; }
///     public bool IsNew() =&gt; Id == Guid.Empty;
/// }
/// </code>
/// The Domain package's <c>EntityBase&lt;TId&gt;</c> provides this default-value convention.
/// </example>
public interface IEntity<TId>
    : IEntity
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    /// <value>The persistent identifier, or its default value when the entity is new.</value>
    TId Id { get; }
}
