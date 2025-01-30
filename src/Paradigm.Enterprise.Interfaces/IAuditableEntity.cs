using System;

namespace Paradigm.Enterprise.Interfaces;

public interface IAuditableEntity : IEntity
{
    /// <summary>
    /// Gets or sets the creation user identifier.
    /// </summary>
    /// <value>
    /// The creation user identifier.
    /// </value>
    int? CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the creation date.
    /// </summary>
    /// <value>
    /// The creation date.
    /// </value>
    DateTimeOffset CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the modified by user identifier.
    /// </summary>
    /// <value>
    /// The modified by user identifier.
    /// </value>
    int? ModifiedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the modification date.
    /// </summary>
    /// <value>
    /// The modification date.
    /// </value>
    DateTimeOffset? ModificationDate { get; set; }
}