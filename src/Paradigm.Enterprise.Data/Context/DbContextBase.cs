using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Services;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Data.Context
{
    public class DbContextBase : DbContext, ICommiteable
    {
        #region Properties

        /// <summary>
        /// The service provider
        /// </summary>
        protected readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextBase" /> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="options">The options for this context.</param>
        /// <remarks>
        /// See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see> and
        /// <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see> for more information and examples.
        /// </remarks>
        public DbContextBase(IServiceProvider serviceProvider, DbContextOptions options) : base(options)
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
        /// <returns></returns>
        public ITransaction CreateTransaction() => new DbContextTransaction(Database);

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="T:System.Threading.CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>
        /// A task that represents the asynchronous save operation. The task result contains the
        /// number of state entries written to the database.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method will automatically call <see cref="M:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.DetectChanges" />
        /// to discover any changes to entity instances before saving to the underlying database. This can be disabled via
        /// <see cref="P:Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.AutoDetectChangesEnabled" />.
        /// </para>
        /// <para>
        /// Entity Framework Core does not support multiple parallel operations being run on the same DbContext instance. This
        /// includes both parallel execution of async queries and any explicit concurrent use from multiple threads.
        /// Therefore, always await async calls immediately, or use separate DbContext instances for operations that execute
        /// in parallel. See <see href="https://aka.ms/efcore-docs-threading">Avoiding DbContext threading issues</see> for more
        /// information and examples.
        /// </para>
        /// <para>
        /// See <see href="https://aka.ms/efcore-docs-saving-data">Saving data in EF Core</see> for more information and examples.
        /// </para>
        /// </remarks>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            IEntity? loggedUser = null;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                loggedUser = loggedUser ?? _serviceProvider.GetRequiredService<ILoggedUserService>().TryGetAuthenticatedUser<IEntity>();

                if (loggedUser is null) continue;

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

        #endregion

        #region Protected Methods

        /// <summary>
        /// Audits the entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="loggedUserId">The logged user identifier.</param>
        protected virtual void AuditEntity(IAuditableEntity entity, int loggedUserId)
        {
            entity.Audit(loggedUserId);
        }

        #endregion
    }
}