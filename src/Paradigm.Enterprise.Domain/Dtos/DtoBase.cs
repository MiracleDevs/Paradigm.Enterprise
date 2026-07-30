namespace Paradigm.Enterprise.Domain.Dtos
{
    /// <summary>
    /// Provides the conventional integer identity and new-object check for data transfer objects.
    /// </summary>
    public abstract class DtoBase
    {
        /// <summary>
        /// Gets or sets the persistent identifier.
        /// </summary>
        /// <value>
        /// Zero for a new object; otherwise, the persistent identifier.
        /// </value>
        public int Id { get; set; }

        /// <summary>
        /// Determines whether the object has not yet been assigned an identifier.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance is new; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsNew() => Id == default;
    }
}
