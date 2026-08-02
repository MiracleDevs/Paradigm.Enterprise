using BeaconAr.Domain.Access.Contracts;

namespace BeaconAr.Domain.Receivables.Generated;

public partial class ApplicationUser
{
    #region Public Methods

    public static ApplicationUser Create(AuthenticatedIdentity identity, DateTimeOffset now) => new()
    {
        Issuer = identity.Issuer,
        Subject = identity.Subject,
        DisplayName = identity.DisplayName,
        Email = identity.Email,
        IsActive = true,
        CreationDate = now,
    };

    public bool Synchronize(AuthenticatedIdentity identity, DateTimeOffset now)
    {
        if (DisplayName == identity.DisplayName && Email == identity.Email)
            return false;

        DisplayName = identity.DisplayName;
        Email = identity.Email;
        ModifiedByUserId = Id;
        ModificationDate = now;
        return true;
    }

    #endregion
}
