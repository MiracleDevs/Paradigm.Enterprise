using BeaconAr.Domain.Access.Application;

namespace BeaconAr.Domain.Access.Contracts;

public sealed record AuthenticatedIdentity(
    string Issuer,
    string Subject,
    string DisplayName,
    string? Email,
    IReadOnlyList<string> Policies)
{
    #region Public Methods

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || Issuer.Length > 400 ||
            string.IsNullOrWhiteSpace(Subject) || Subject.Length > 200 ||
            string.IsNullOrWhiteSpace(DisplayName) || DisplayName.Length > 200 ||
            Email?.Length > 320)
        {
            throw new AccessException("forbidden", "The authenticated identity is incomplete.");
        }
    }

    #endregion
}
