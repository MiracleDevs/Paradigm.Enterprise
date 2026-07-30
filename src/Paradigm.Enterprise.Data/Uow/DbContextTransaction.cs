using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Paradigm.Enterprise.Domain.Uow;
using System.Data;

namespace Paradigm.Enterprise.Data.Uow
{
    /// <summary>
    /// Wraps an Entity Framework transaction and enlists compatible contexts and commands in it.
    /// </summary>
    /// <remarks>
    /// Enlisted contexts must use the same connection as the originating context. After a successful
    /// commit, rollback, or transaction disposal, all contexts enlisted through this wrapper are detached.
    /// If the underlying operation throws, detachment is skipped.
    /// </remarks>
    /// <example>
    /// Applications receive this wrapper through <see cref="ICommiteable.CreateTransaction"/> rather
    /// than constructing it directly:
    /// <code>
    /// using ITransaction transaction = unitOfWork.CreateTransaction();
    /// try
    /// {
    ///     await repository.UpdateAsync(entity);
    ///     await unitOfWork.CommitChangesAsync();
    ///     transaction.Commit();
    /// }
    /// catch
    /// {
    ///     if (transaction.IsActive)
    ///         transaction.Rollback();
    ///     throw;
    /// }
    /// </code>
    /// A raw command can participate after its connection has been set to the same connection:
    /// <code>
    /// command.Connection = context.Database.GetDbConnection();
    /// unitOfWork.UseTransaction(command);
    /// await command.ExecuteNonQueryAsync();
    /// </code>
    /// This type is not a distributed transaction coordinator and cannot enlist unrelated connections.
    /// </example>
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
        /// Commits the underlying transaction and, on success, detaches enlisted contexts.
        /// </summary>
        /// <remarks>
        /// A commit does not call <see cref="ICommiteable.CommitChangesAsync"/>. Save staged context
        /// changes before invoking this method.
        /// </remarks>
        public void Commit()
        {
            Transaction.Commit();
            DettachDbContexts();
        }

        /// <summary>
        /// Rolls back the underlying transaction and, on success, detaches enlisted contexts.
        /// </summary>
        public void Rollback()
        {
            Transaction.Rollback();
            DettachDbContexts();
        }

        /// <summary>
        /// Disposes the underlying transaction and, on success, detaches enlisted contexts.
        /// </summary>
        /// <remarks>Disposal does not commit staged or transactional work.</remarks>
        public void Dispose()
        {
            Transaction.Dispose();
            DettachDbContexts();
        }

        /// <summary>
        /// Enlists a compatible Entity Framework context in the transaction.
        /// </summary>
        /// <param name="commiteable">The participant to inspect and enlist.</param>
        /// <remarks>
        /// Participants that are not <see cref="DbContext"/> instances are ignored. A context is
        /// attached only once and must use the same connection as the underlying transaction.
        /// </remarks>
        public void AddCommiteable(ICommiteable commiteable)
        {
            if (commiteable is DbContext dbContext && !AttachedDbContexts.Contains(dbContext))
            {
                dbContext.Database.UseTransaction(Transaction.GetDbTransaction());
                AttachedDbContexts.Add(dbContext);
            }
        }

        /// <summary>
        /// Associates a database command with the underlying transaction.
        /// </summary>
        /// <param name="command">
        /// The command to enlist. Its connection must match the transaction's connection.
        /// </param>
        /// <remarks>This method replaces any transaction currently assigned to the command.</remarks>
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
