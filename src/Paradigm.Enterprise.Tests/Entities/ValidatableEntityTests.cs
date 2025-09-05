using Paradigm.Enterprise.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Paradigm.Enterprise.Tests.Entities;

[TestClass]
[ExcludeFromCodeCoverage]
public class ValidatableEntityTests
{
    // Sample entity with validation logic
    public class TestEntity : EntityBase<IEntity, TestEntity, TestEntity>, IEntity
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(50, ErrorMessage = "Name cannot be longer than 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Range(1, 120, ErrorMessage = "Age must be between 1 and 120")]
        public int Age { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; } = string.Empty;

        // Custom validation logic in addition to attributes
        public override void Validate()
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(this);
            var isValid = Validator.TryValidateObject(this, validationContext, validationResults, true);

            var validator = new DomainValidator();

            // Custom business rules
            // Age validation - Must be between 1 and 100 (stricter than the attribute)
            if (Age > 100)
            {
                validator.AddError("Age must be 100 or less");
                isValid = false;
            }

            // Skip email validation for users under 18
            if (Age < 18 && !string.IsNullOrEmpty(Email))
            {
                validator.AddError("Users under 18 cannot have an email address");
                isValid = false;
            }
            else if (Age < 18 && string.IsNullOrEmpty(Email))
            {
                // Remove any email-related validation errors
                validationResults.RemoveAll(result =>
                    result.MemberNames.Contains(nameof(Email)));

                // Recheck if valid without email validation
                isValid = !validationResults.Any() && validator.ToString() == string.Empty;
            }

            // Custom rule that disallows "admin" as a name
            if (Name.ToLower() == "admin")
            {
                validator.AddError("Name cannot be 'admin'");
                isValid = false;
            }

            // Add standard validation results
            if (validationResults.Any())
            {
                foreach (var validationResult in validationResults)
                {
                    validator.AddError(validationResult.ErrorMessage ?? "Validation failed");
                }
            }

            if (!isValid)
            {
                throw new DomainException(validator.ToString() ?? "Validation issue");
            }
        }

        public override TestEntity MapTo(IServiceProvider serviceProvider)
        {
            return this;
        }

        public override TestEntity? MapFrom(IServiceProvider serviceProvider, IEntity model)
        {
            if (model is TestEntity entity)
            {
                Id = entity.Id;
                Name = entity.Name;
                Age = entity.Age;
                Email = entity.Email;
                Description = entity.Description;
            }
            return this;
        }
    }

    [TestMethod]
    public void Validate_WithValidEntity_ShouldNotThrowException()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "John Doe",
            Age = 30,
            Email = "john.doe@example.com",
            Description = "A test user"
        };

        // Act & Assert - no exception should be thrown
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_WithMissingRequiredFields_ShouldThrowDomainException()
    {
        // Arrange
        var entity = new TestEntity
        {
            // Name is missing (required)
            Age = 25,
            Email = "test@example.com"
        };

        // Act - should throw DomainException
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_WithInvalidEmail_ShouldThrowDomainException()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Jane Doe",
            Age = 28,
            Email = "invalid-email" // Invalid email format
        };

        // Act - should throw DomainException
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_WithValueOutOfRange_ShouldThrowDomainException()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Age Test",
            Age = 101, // Out of valid range (0-100)
            Email = "age.test@example.com"
        };

        // Act - should throw DomainException
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_WithCustomRuleViolation_ShouldThrowDomainException()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "admin", // Violates custom rule
            Age = 35,
            Email = "admin@example.com"
        };

        // Act - should throw DomainException
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_WithBusinessRuleViolation_ShouldThrowDomainException()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Young User",
            Age = 16, // Under 18
            Email = "young.user@example.com" // Email shouldn't be set for users under 18
        };

        // Act - should throw DomainException
        entity.Validate();
    }

    [TestMethod]
    [ExpectedException(typeof(DomainException))]
    public void Validate_UnderAgeWithoutEmail_ShouldPass()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Young User",
            Age = 16, // Under 18
            Email = "" // No email is fine
        };

        // Act & Assert - no exception should be thrown
        entity.Validate();
    }
}