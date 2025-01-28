using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Domain.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class NotEmptyAttribute : ValidationAttribute
    {
        public const string DefaultErrorMessage = "The {0} field must not be empty";

        public NotEmptyAttribute() : base(DefaultErrorMessage) { }

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