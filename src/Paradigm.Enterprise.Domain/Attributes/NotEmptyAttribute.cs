using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Attributes
{
    /// <summary>
    /// Validates that a value is not the empty or default representation supported by this attribute.
    /// </summary>
    /// <remarks>
    /// <see langword="null"/>, zero <see cref="int"/> values, empty GUIDs, default <see cref="DateTimeOffset"/> values,
    /// blank strings, and empty collections are invalid. Other value types are accepted.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class NotEmptyAttribute : ValidationAttribute
    {
        /// <summary>
        /// The default validation message used when a value is empty.
        /// </summary>
        public const string DefaultErrorMessage = "The {0} field must not be empty";

        /// <summary>
        /// Initializes an instance using <see cref="DefaultErrorMessage"/>.
        /// </summary>
        public NotEmptyAttribute() : base(DefaultErrorMessage) { }

        /// <summary>
        /// Determines whether <paramref name="value"/> has a nonempty supported representation.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns><see langword="false"/> for a recognized empty value; otherwise, <see langword="true"/>.</returns>
        public override bool IsValid(object? value)
        {
            return value switch
            {
                null => false,
                int id => id != default,
                Guid guid => guid != Guid.Empty,
                DateTimeOffset date => date != default,
                string strValue => !string.IsNullOrWhiteSpace(strValue),
                ICollection collection => collection.Count > 0,
                _ => true,
            };
        }
    }
}
