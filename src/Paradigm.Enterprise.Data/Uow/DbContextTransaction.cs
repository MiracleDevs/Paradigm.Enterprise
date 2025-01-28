using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;

namespace Paradigm.Enterprise.Data.Uow
{
    public class DbContextTransaction : ITransaction
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        public bool IsActive => Transaction.GetDbTransaction().Connection is not null;

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        private IDbContextTransaction Transaction { get; }

        /// <summary>
        /// Gets the attached commiteables.
        /// </summary>
        /// <value>
        /// The attached commiteables.
        /// </value>
        private List<DbContext> AttachedDbContexts { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DbContextTransaction"/> class.
        /// </summary>
        /// <param name="databaseConnection">The database connection.</param>
        internal DbContextTransaction(DatabaseFacade databaseConnection)
        {
            Transaction = databaseConnection.BeginTransaction();
            AttachedDbContexts = [];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Commits this instance.
        /// </summary>
        public void Commit()
        {
            Transaction.Commit();
            DettachDbContexts();
        }

        /// <summary>
        /// Rollbacks this instance.
        /// </summary>
        public void Rollback()
        {
            Transaction.Rollback();
            DettachDbContexts();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Transaction.Dispose();
            DettachDbContexts();
        }

        /// <summary>
        /// Adds the commiteable.
        /// </summary>
        /// <param name="commiteable">The commiteable.</param>
        public void AddCommiteable(ICommiteable commiteable)
        {
            if (commiteable is DbContext dbContext && !AttachedDbContexts.Contains(dbContext))
            {
                dbContext.Database.UseTransaction(Transaction.GetDbTransaction());
                AttachedDbContexts.Add(dbContext);
            }
        }

        /// <summary>
        /// Adds the command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void AddCommand(IDbCommand command)
        {
            command.Transaction = Transaction.GetDbTransaction();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Dettaches the database contexts.
        /// </summary>
        private void DettachDbContexts()
        {
            foreach (var dbContext in AttachedDbContexts)
                dbContext.Database.UseTransaction(null);

            AttachedDbContexts.Clear();
        }

        #endregion
    }
}