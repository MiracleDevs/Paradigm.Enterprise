namespace Paradigm.Enterprise.Cli;

internal interface IValidationRule
{
    string Code { get; }

    IEnumerable<Diagnostic> Evaluate(ValidationContext context);
}