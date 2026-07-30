using System;

namespace Paradigm.Enterprise.Interfaces;

/// <summary>
/// Defines an entity that records the users responsible for its creation and latest modification.
/// </summary>
/// <typeparam name="TId">The value type used for entity and user identifiers.</typeparam>
/// <remarks>
/// The interface stores audit data but does not populate it. Applications can assign the members
/// directly or use the Domain package's <c>Audit</c> extensions for supported timestamp types.
/// </remarks>
public interface IAuditableEntity<TId> : IEntity<TId>
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets or sets the creation user identifier.
    /// </summary>
    /// <value>
    /// The creation user identifier.
    /// </value>
    TId? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the modified by user identifier.
    /// </summary>
    /// <value>
    /// The modified by user identifier.
    /// </value>
    TId? ModifiedByUserId { get; set; }
}

/// <summary>
/// Defines an auditable entity that records both responsible users and audit timestamps.
/// </summary>
/// <typeparam name="TDate">The value type used to represent audit timestamps.</typeparam>
/// <typeparam name="TId">The value type used for entity and user identifiers.</typeparam>
/// <remarks>
/// The concrete application determines the clock representation. The domain audit extensions provide
/// built-in UTC handling for <see cref="DateTime"/> and <see cref="DateTimeOffset"/>.
/// </remarks>
public interface IAuditableEntity<TDate, TId> : IAuditableEntity<TId>
    where TDate : struct
    where TId : struct, IEquatable<TId>
{
    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    /// <value>
    /// The creation date.
    /// </value>
    TDate CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the modification date.
    /// </summary>
    /// <value>
    /// The modification date.
    /// </value>
    TDate? ModificationDate { get; set; }
}
