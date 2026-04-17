using System;

namespace Paradigm.Enterprise.Interfaces;

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