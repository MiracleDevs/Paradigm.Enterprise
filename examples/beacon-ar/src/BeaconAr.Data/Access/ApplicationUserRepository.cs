using BeaconAr.Data.Receivables;
using BeaconAr.Domain.Access.Contracts;
using BeaconAr.Domain.Access.Repositories;
using BeaconAr.Domain.Receivables.Generated;
using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Repositories;

namespace BeaconAr.Data.Access;

public sealed class ApplicationUserRepository : RepositoryBase<ReceivablesDbContext, int>, IApplicationUserRepository
{
    #region Constructors

    public ApplicationUserRepository(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
    }

    #endregion

    #region Public Methods

    public Task<ApplicationUser?> FindAsync(string issuer, string subject, CancellationToken cancellationToken) =>
        EntityContext.ApplicationUsers.SingleOrDefaultAsync(
            user => user.Issuer == issuer && user.Subject == subject,
            cancellationToken);

    public async Task<ApplicationUser> GetOrCreateAsync(
        AuthenticatedIdentity identity,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await EntityContext.Database.ExecuteSqlInterpolatedAsync($$"""
            SET XACT_ABORT ON;
            BEGIN TRANSACTION;
            IF NOT EXISTS
            (
                SELECT 1
                FROM [dbo].[ApplicationUser] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Issuer] = {{identity.Issuer}} AND [Subject] = {{identity.Subject}}
            )
            BEGIN
                INSERT INTO [dbo].[ApplicationUser]
                    ([Issuer], [Subject], [DisplayName], [Email], [IsActive], [CreationDate])
                VALUES
                    ({{identity.Issuer}}, {{identity.Subject}}, {{identity.DisplayName}}, {{identity.Email}}, 1, {{now}});
            END;
            COMMIT TRANSACTION;
            """, cancellationToken);

        return await EntityContext.ApplicationUsers.SingleAsync(
            user => user.Issuer == identity.Issuer && user.Subject == identity.Subject,
            cancellationToken);
    }

    #endregion
}
