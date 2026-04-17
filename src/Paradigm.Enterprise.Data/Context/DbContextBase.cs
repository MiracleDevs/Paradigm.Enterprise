using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Services;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Data.Context
{
    public class DbContextBase<TId> : DbContext, ICommiteable
        where TId : struct, IEquatable<TId>
    {
        #region Properties

        /// <summary>
        /// The service provider
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructor

        public DbContextBase(IServiceProvider serviceProvider, DbContextOptions options)
            : base(options)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Commits the changes.
        /// </summary>
        public async Task CommitChangesAsync()
        {
            await SaveChangesAsync();
        }

        /// <summary>
        /// Creates the transaction.
        /// </summary>
        public ITransaction CreateTransaction() => new DbContextTransaction(Database);

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            IEntity<TId>? loggedUser = null;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity<TId>>())
            {
                loggedUser ??= _serviceProvider
                    .GetRequiredService<ILoggedUserService<TId>>()
                    .TryGetAuthenticatedUser<IEntity<TId>>();

                if (loggedUser is null)
                    continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                    case EntityState.Modified:
                        AuditEntity(entry.Entity, loggedUser.Id);
                        break;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Audits the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="loggedUserId">The logged user identifier.</param>
        protected virtual void AuditEntity(IAuditableEntity<TId> entity, TId loggedUserId)
        {
            entity.Audit(loggedUserId);
        }

        #endregion
    }
}