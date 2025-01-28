using Microsoft.EntityFrameworkCore;
using Paradigm.Enterprise.Data.Uow;
using Paradigm.Enterprise.Domain.Uow;

namespace Paradigm.Enterprise.Data.Context
{
    public class DbContextBase : DbContext, ICommiteable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextBase"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <remarks>
        /// See <see href="https://aka.ms/efcore-docs-dbcontext">DbContext lifetime, configuration, and initialization</see> and
        /// <see href="https://aka.ms/efcore-docs-dbcontext-options">Using DbContextOptions</see> for more information and examples.
        /// </remarks>
        public DbContextBase(DbContextOptions options) : base(options)
        {
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

        #endregion
    }
}