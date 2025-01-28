using Paradigm.Enterprise.Domain.Uow;
using System.Data;

namespace Paradigm.Enterprise.Data.Uow
{
    public class UnitOfWork : IUnitOfWork
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance has active transaction.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has active transaction; otherwise, <c>false</c>.
        /// </value>
        public bool HasActiveTransaction => CurrentTransaction?.IsActive ?? false;

        /// <summary>
        /// Gets the commiteable repositories.
        /// </summary>
        private List<ICommiteable> Commiteables { get; }

        /// <summary>
        /// Gets or sets the current transaction.
        /// </summary>
        /// <value>
        /// The current transaction.
        /// </value>
        private ITransaction? CurrentTransaction { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
        /// </summary>
        public UnitOfWork()
        {
            Commiteables = [];
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Commits the changes.
        /// </summary>
        public async Task CommitChangesAsync()
        {
            foreach (var commiteable in Commiteables)
                await commiteable.CommitChangesAsync();
        }

        /// <summary>
        /// Registers the commiteable.
        /// </summary>
        /// <param name="commiteable">The commiteable.</param>
        /// <exception cref="ArgumentNullException">commiteable</exception>
        public void RegisterCommiteable(ICommiteable commiteable)
        {
            if (commiteable is null)
                throw new ArgumentNullException(nameof(commiteable));

            if (CurrentTransaction?.IsActive ?? false)
                CurrentTransaction.AddCommiteable(commiteable);

            if (!Commiteables.Contains(commiteable))
                Commiteables.Add(commiteable);
        }

        /// <summary>
        /// Creates the transaction.
        /// </summary>
        /// <returns>
        /// A new transaction.
        /// </returns>
        public ITransaction CreateTransaction()
        {
            if (!Commiteables.Any())
                throw new InvalidOperationException("No commiteable objects have been registered.");

            if (HasActiveTransaction)
                throw new InvalidOperationException("A transaction is already opened.");

            CurrentTransaction = Commiteables[0].CreateTransaction();

            if (Commiteables.Count > 1)
                for (var i = 1; i < Commiteables.Count; i++)
                    CurrentTransaction.AddCommiteable(Commiteables[i]);

            return CurrentTransaction;
        }

        /// <summary>
        /// Uses the current transaction.
        /// </summary>
        /// <exception cref="InvalidOperationException">No transaction is active.</exception>
        public ITransaction UseCurrentTransaction()
        {
            if (CurrentTransaction is null || !HasActiveTransaction)
                throw new InvalidOperationException("No transaction is active.");

            UseTransaction(CurrentTransaction);
            return CurrentTransaction;
        }

        /// <summary>
        /// Uses the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        public void UseTransaction(ITransaction transaction)
        {
            foreach (var commiteable in Commiteables)
                transaction.AddCommiteable(commiteable);
        }

        /// <summary>
        /// Uses the transaction.
        /// </summary>
        /// <param name="command">The command.</param>
        public void UseTransaction(IDbCommand command)
        {
            if (!HasActiveTransaction) return;
            CurrentTransaction?.AddCommand(command);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var commiteable in Commiteables)
                if (commiteable is IDisposable disposable)
                    disposable.Dispose();

            CurrentTransaction?.Dispose();
        }

        #endregion
    }
}