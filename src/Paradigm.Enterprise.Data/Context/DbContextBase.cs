using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Extensions;
using Paradigm.Enterprise.Domain.Services;
using Paradigm.Enterprise.Domain.Uow;
using Paradigm.Enterprise.Interfaces;

namespace Paradigm.Enterprise.Data.Context
{
    /// <summary>
    /// Extends an Entity Framework context with unit-of-work participation and automatic entity auditing.
    /// </summary>
    /// <typeparam name="TId">The value type used for entity and logged-user identifiers.</typeparam>
    /// <remarks>
    /// Before saving, added and modified <see cref="IAuditableEntity{TId}"/> entries are audited when
    /// an authenticated user is available from the current service scope. Changes are persisted only
    /// when <see cref="SaveChangesAsync(CancellationToken)"/> or <see cref="CommitChangesAsync"/> is called.
    /// The context is normally scoped to a request and registered with the same identifier type used by
    /// the application's entities and <see cref="ILoggedUserService{TId}"/>.
    /// </remarks>
    /// <example>
    /// Define the application context and pass both dependencies to the base constructor:
    /// <code>
    /// public sealed class SalesDbContext : DbContextBase&lt;int&gt;
    /// {
    ///     public SalesDbContext(
    ///         IServiceProvider services,
    ///         DbContextOptions&lt;SalesDbContext&gt; options)
    ///         : base(services, options)
    ///     {
    ///     }
    ///
    ///     public DbSet&lt;Order&gt; Orders =&gt; Set&lt;Order&gt;();
    ///
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         base.OnModelCreating(modelBuilder);
    ///         modelBuilder.Entity&lt;Order&gt;().HasKey(order =&gt; order.Id);
    ///     }
    /// }
    /// </code>
    /// Register the context, unit of work, and logged-user service in the same scope:
    /// <code>
    /// services.AddDbContext&lt;SalesDbContext&gt;(
    ///     options =&gt; options.UseSqlServer(connectionString));
    /// services.AddScoped&lt;IUnitOfWork, UnitOfWork&gt;();
    /// services.AddScoped&lt;ILoggedUserService&lt;int&gt;, LoggedUserService&gt;();
    /// </code>
    /// Repositories stage changes in this context. A provider can then persist all registered contexts
    /// through <c>await unitOfWork.CommitChangesAsync()</c>. Calling <see cref="SaveChangesAsync(CancellationToken)"/>
    /// directly is also supported but bypasses coordination with other unit-of-work participants.
    /// </example>
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

        /// <summary>
        /// Initializes the context with its scoped services and Entity Framework options.
        /// </summary>
        /// <param name="serviceProvider">The scoped provider used to resolve the logged-user service.</param>
        /// <param name="options">The options that configure this context.</param>
        public DbContextBase(IServiceProvider serviceProvider, DbContextOptions options)
            : base(options)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Persists this context's tracked changes as a unit-of-work participant.
        /// </summary>
        /// <returns>A task that completes after Entity Framework saves the tracked changes.</returns>
        /// <remarks>
        /// This is the <see cref="ICommiteable"/> entry point used by <see cref="UnitOfWork"/>.
        /// It delegates to <see cref="SaveChangesAsync(CancellationToken)"/>, so auditing runs before
        /// persistence. It does not begin or commit a database transaction.
        /// </remarks>
        public async Task CommitChangesAsync()
        {
            await SaveChangesAsync();
        }

        /// <summary>
        /// Begins a relational database transaction for this context.
        /// </summary>
        /// <returns>
        /// A transaction wrapper that can enlist other compatible contexts and database commands.
        /// </returns>
        /// <remarks>
        /// The returned transaction owns the underlying Entity Framework transaction. Commit, roll back,
        /// or dispose it. Enlisted contexts and commands must use the same relational connection.
        /// </remarks>
        public ITransaction CreateTransaction() => new DbContextTransaction(Database);

        /// <summary>
        /// Audits eligible tracked entities and then persists all tracked changes.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the asynchronous operation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        /// <remarks>
        /// For each added or modified auditable entity, the logged-user service is resolved lazily and
        /// queried for the authenticated user. If it returns <see langword="null"/>, the entity is saved
        /// without an audit update. Deleted and unchanged entities are not audited by this method.
        /// Exceptions from auditing, service resolution, or Entity Framework are propagated.
        /// </remarks>
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
        /// Applies audit data to an added or modified entity before it is saved.
        /// </summary>
        /// <param name="entity">The tracked auditable entity being saved.</param>
        /// <param name="loggedUserId">The identifier of the authenticated user performing the change.</param>
        /// <remarks>
        /// The default implementation calls the domain <c>Audit</c> extension for
        /// <see cref="IAuditableEntity{TId}"/>.
        /// Override this hook to add application-specific audit behavior, and call the base implementation
        /// when the entity's standard audit fields should still be populated.
        /// </remarks>
        protected virtual void AuditEntity(IAuditableEntity<TId> entity, TId loggedUserId)
        {
            entity.Audit(loggedUserId);
        }

        #endregion
    }
}
