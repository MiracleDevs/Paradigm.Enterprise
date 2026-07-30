using Paradigm.Enterprise.Domain.Uow;
using System.Data;

namespace Paradigm.Enterprise.Data.Uow
{
    /// <summary>
    /// Coordinates commits and an optional shared transaction across registered persistence participants.
    /// </summary>
    /// <remarks>
    /// <see cref="CommitChangesAsync"/> invokes participants in registration order. It does not create
    /// a transaction automatically; call <see cref="CreateTransaction"/> when atomicity is required.
    /// </remarks>
    /// <example>
    /// Register the unit of work as scoped so repositories resolved in one request share it:
    /// <code>
    /// services.AddScoped&lt;IUnitOfWork, UnitOfWork&gt;();
    /// services.AddDbContext&lt;SalesDbContext&gt;(...);
    /// services.AddScoped&lt;IOrderRepository, OrderRepository&gt;();
    /// </code>
    /// Repository constructors register their contexts automatically. Stage all work before committing:
    /// <code>
    /// await orderRepository.AddAsync(order);
    /// await auditRepository.AddAsync(auditEntry);
    /// await unitOfWork.CommitChangesAsync();
    /// </code>
    /// To coordinate compatible participants in one relational transaction, explicitly create and
    /// complete the transaction:
    /// <code>
    /// using var transaction = unitOfWork.CreateTransaction();
    /// try
    /// {
    ///     await orderRepository.UpdateAsync(order);
    ///     await auditRepository.AddAsync(auditEntry);
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
    /// Creating the transaction does not save or commit staged changes. All enlisted contexts and
    /// commands must use a connection compatible with the transaction created by the first participant.
    /// </example>
    public class UnitOfWork : IUnitOfWork
    {
        #region Properties

        /// <summary>
        /// Gets whether the transaction created by this unit of work is still active.
        /// </summary>
        /// <value>
        /// <see langword="true"/> when the current transaction exists and reports itself active;
        /// otherwise, <see langword="false"/>.
        /// </value>
        public bool HasActiveTransaction => CurrentTransaction?.IsActive ?? false;

        /// <summary>
        /// Gets the persistence participants in registration order.
        /// </summary>
        private List<ICommiteable> Commiteables { get; }

        /// <summary>
        /// Gets or sets the transaction created by this unit of work.
        /// </summary>
        /// <value>
        /// The current transaction, or <see langword="null"/> before one is created.
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
        /// Persists staged changes for every registered participant, sequentially in registration order.
        /// </summary>
        /// <returns>A task that completes after every participant has committed its changes.</returns>
        /// <remarks>
        /// This method does not commit or create the database transaction itself. If a participant fails,
        /// later participants are not invoked and the exception is propagated to the caller.
        /// </remarks>
        public async Task CommitChangesAsync()
        {
            foreach (var commiteable in Commiteables)
                await commiteable.CommitChangesAsync();
        }

        /// <summary>
        /// Registers a persistence participant for future commits and transaction enlistment.
        /// </summary>
        /// <param name="commiteable">
        /// The participant to register. The same instance is stored only once; when a transaction is
        /// already active, the participant is also offered to that transaction for enlistment.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="commiteable"/> is <see langword="null"/>.</exception>
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
        /// Begins a transaction through the first registered participant and enlists the remaining participants.
        /// </summary>
        /// <returns>
        /// The newly created active transaction.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No persistence participants are registered, or a transaction created by this unit of work is already active.
        /// </exception>
        /// <remarks>
        /// All participants must be compatible with the transaction created by the first registered participant.
        /// The method does not commit staged changes.
        /// </remarks>
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
        /// Re-enlists every registered participant in the transaction created by this unit of work.
        /// </summary>
        /// <returns>The current active transaction.</returns>
        /// <exception cref="InvalidOperationException">This unit of work has no active current transaction.</exception>
        public ITransaction UseCurrentTransaction()
        {
            if (CurrentTransaction is null || !HasActiveTransaction)
                throw new InvalidOperationException("No transaction is active.");

            UseTransaction(CurrentTransaction);
            return CurrentTransaction;
        }

        /// <summary>
        /// Enlists every registered participant in the supplied transaction.
        /// </summary>
        /// <param name="transaction">The transaction that receives the registered participants.</param>
        /// <remarks>
        /// This overload does not assign <paramref name="transaction"/> as the unit of work's current
        /// transaction; consequently it does not change <see cref="HasActiveTransaction"/>.
        /// </remarks>
        public void UseTransaction(ITransaction transaction)
        {
            foreach (var commiteable in Commiteables)
                transaction.AddCommiteable(commiteable);
        }

        /// <summary>
        /// Associates a database command with the current transaction when that transaction is active.
        /// </summary>
        /// <param name="command">The command to enlist in the current transaction.</param>
        /// <remarks>
        /// When there is no active current transaction, the command is left unchanged.
        /// The command and transaction must use compatible database connections.
        /// </remarks>
        public void UseTransaction(IDbCommand command)
        {
            if (!HasActiveTransaction) return;
            CurrentTransaction?.AddCommand(command);
        }

        /// <summary>
        /// Disposes all disposable participants in registration order and then disposes the current transaction.
        /// </summary>
        /// <remarks>
        /// Disposal does not commit staged changes. The implementation does not clear registrations or guarantee
        /// idempotency; callers should dispose the unit of work once, after its repositories are no longer needed.
        /// </remarks>
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
