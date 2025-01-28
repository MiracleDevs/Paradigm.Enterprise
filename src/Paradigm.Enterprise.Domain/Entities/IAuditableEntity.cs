using Paradigm.Enterprise.Domain.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Entities
{
    public interface IAuditableEntity : Interfaces.IEntity
    {
        /// <summary>
        /// Gets or sets the creation user identifier.
        /// </summary>
        /// <value>
        /// The creation user identifier.
        /// </value>
        [Required]
        [NotEmpty]
        int? CreatedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        /// <value>
        /// The creation date.
        /// </value>
        [Required]
        [NotEmpty]
        DateTimeOffset CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the modified by user identifier.
        /// </summary>
        /// <value>
        /// The modified by user identifier.
        /// </value>
        [NotEmpty]
        int? ModifiedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the modification date.
        /// </summary>
        /// <value>
        /// The modification date.
        /// </value>
        DateTimeOffset? ModificationDate { get; set; }
    }
}