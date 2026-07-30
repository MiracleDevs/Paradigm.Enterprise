using System.Text;

namespace Paradigm.Enterprise.Domain.Exceptions;

/// <summary>
/// Accumulates domain-rule failures and raises them as a single <see cref="DomainException"/>.
/// </summary>
/// <remarks>
/// Assertions do not throw immediately. Call <see cref="ThrowIfAny"/> after evaluating related rules
/// to present all accumulated messages together. A validator is intended for one validation pass:
/// <see cref="ThrowIfAny"/> does not clear accumulated messages.
/// </remarks>
/// <example>
/// A domain entity can report several correctable problems in one failure:
/// <code>
/// public override void Validate()
/// {
///     var rules = new DomainValidator();
///     rules.Assert(Lines.Count != 0, "An order requires at least one line.");
///     rules.Assert(Total &gt;= 0, "The order total cannot be negative.");
///
///     if (CustomerIsOnHold)
///         rules.AddError("The customer account is on hold.");
///
///     rules.ThrowIfAny();
/// }
///
/// try
/// {
///     order.Validate();
/// }
/// catch (DomainException exception)
/// {
///     // exception.Message contains every failed rule, separated by line breaks.
///     validationProblem = exception.Message;
/// }
/// </code>
/// </example>
public class DomainValidator
{
    /// <summary>
    /// Gets or sets the message builder.
    /// </summary>
    /// <value>
    /// The message builder.
    /// </value>
    private StringBuilder MessageBuilder { get; }

    /// <summary>
    /// Initializes an empty validator.
    /// </summary>
    public DomainValidator()
    {
        MessageBuilder = new StringBuilder();
    }

    /// <summary>
    /// Appends an error message to the current validation result.
    /// </summary>
    /// <param name="message">The domain-rule failure message.</param>
    public void AddError(string message)
    {
        MessageBuilder.AppendLine(message);
    }

    /// <summary>
    /// Appends an error when the specified condition is <see langword="false"/>.
    /// </summary>
    /// <param name="condition">The domain condition that must hold.</param>
    /// <param name="message">The error message if the condition fails.</param>
    public void Assert(bool condition, string message)
    {
        if (!condition)
            AddError(message);
    }

    /// <summary>
    /// Throws a single exception containing all accumulated error messages.
    /// </summary>
    /// <exception cref="DomainException">One or more errors have been accumulated.</exception>
    public void ThrowIfAny()
    {
        if (MessageBuilder.Length != 0)
            throw new DomainException(MessageBuilder.ToString());
    }
}
