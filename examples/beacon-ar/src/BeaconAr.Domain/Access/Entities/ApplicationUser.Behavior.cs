using System.Net.Mail;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Interfaces.Access.Entities;
using Paradigm.Enterprise.Domain.Exceptions;

namespace BeaconAr.Domain.Access.Entities;

public partial class ApplicationUser
{
    #region Nested Types

    private sealed record ProposedState(string Issuer, string Subject, string DisplayName, string? Email);

    #endregion

    #region Public Methods

    public static ApplicationUser Create(AuthenticatedIdentity identity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ProposedState state = Normalize(identity.Issuer, identity.Subject, identity.DisplayName, identity.Email);
        ValidateState(state);
        return new ApplicationUser
        {
            Issuer = state.Issuer,
            Subject = state.Subject,
            DisplayName = state.DisplayName,
            Email = state.Email,
            IsActive = true,
            CreationDate = now,
        };
    }

    public bool Synchronize(AuthenticatedIdentity identity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ProposedState state = Normalize(identity.Issuer, identity.Subject, identity.DisplayName, identity.Email);
        ValidateState(state);
        if (!string.Equals(Issuer, state.Issuer, StringComparison.Ordinal) ||
            !string.Equals(Subject, state.Subject, StringComparison.Ordinal))
            throw new DomainException("An authenticated identity cannot change the application user's issuer or subject.");
        if (DisplayName == state.DisplayName && Email == state.Email)
            return false;

        DisplayName = state.DisplayName;
        Email = state.Email;
        ModifiedByUserId = Id;
        ModificationDate = now;
        return true;
    }

    #endregion

    #region Private Methods

    partial void ValidateEntity() => ValidateState(Normalize(Issuer, Subject, DisplayName, Email));

    partial void BeforeMap(IApplicationUser model)
    {
        _ = Id;
        ValidateState(Normalize(model.Issuer, model.Subject, model.DisplayName, model.Email));
    }

    partial void AfterMap(IApplicationUser model)
    {
        ProposedState state = Normalize(Issuer, Subject, DisplayName, Email);
        Issuer = state.Issuer;
        Subject = state.Subject;
        DisplayName = state.DisplayName;
        Email = state.Email;
    }

    private static ProposedState Normalize(string? issuer, string? subject, string? displayName, string? email) => new(
        issuer?.Trim() ?? string.Empty,
        subject?.Trim() ?? string.Empty,
        displayName?.Trim() ?? string.Empty,
        string.IsNullOrWhiteSpace(email) ? null : email.Trim());

    private static void ValidateState(ProposedState state)
    {
        var rules = new DomainValidator();
        rules.Assert(state.Issuer.Length is >= 1 and <= 400, "Issuer is required and cannot exceed 400 characters.");
        rules.Assert(state.Subject.Length is >= 1 and <= 200, "Subject is required and cannot exceed 200 characters.");
        rules.Assert(state.DisplayName.Length is >= 1 and <= 200, "Display name is required and cannot exceed 200 characters.");
        rules.Assert(state.Email is null || state.Email.Length <= 320 && IsValidEmail(state.Email),
            "Email must be syntactically valid and cannot exceed 320 characters.");
        rules.ThrowIfAny();
    }

    private static bool IsValidEmail(string value)
    {
        if (value.Any(char.IsWhiteSpace))
            return false;
        try
        {
            return new MailAddress(value).Address == value;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    #endregion
}
