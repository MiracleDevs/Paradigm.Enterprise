using System.Data;

namespace Paradigm.Enterprise.Domain.Uow
{
    /// <summary>
    /// Coordinates commits and an optional shared transaction across registered persistence participants.
    /// </summary>
    /// <remarks>
    /// Registration and repository operations only stage work. <see cref="ICommiteable.CommitChangesAsync"/>
    /// invokes participants sequentially in registration order and stops at the first failure; earlier
    /// participants may already have saved. An active transaction still controls final commit or rollback.
    /// Transactions can include only participants and commands compatible with the transaction created
    /// by the first registered participant.
    /// </remarks>
    /// <example>
    /// Inject the scoped abstraction into an application provider, stage repository changes, and commit
    /// them once:
    /// <code>
    /// public sealed class CheckoutService
    /// {
    ///     private readonly IOrderRepository _orders;
    ///     private readonly IUnitOfWork _unitOfWork;
    ///
    ///     public CheckoutService(
    ///         IOrderRepository orders,
    ///         IUnitOfWork unitOfWork)
    ///     {
    ///         _orders = orders;
    ///         _unitOfWork = unitOfWork;
    ///     }
    ///
    ///     public async Task PlaceAsync(Order order)
    ///     {
    ///         using ITransaction transaction = _unitOfWork.CreateTransaction();
    ///         try
    ///         {
    ///             await _orders.AddAsync(order);
    ///             await _unitOfWork.CommitChangesAsync();
    ///             transaction.Commit();
    ///         }
    ///         catch
    ///         {
    ///             if (transaction.IsActive)
    ///                 transaction.Rollback();
    ///             throw;
    ///         }
    ///     }
    /// }
    /// </code>
    /// At least one repository or other participant must be resolved and registered before
    /// <see cref="ICommiteable.CreateTransaction"/> is called. Creating or disposing a transaction
    /// does not implicitly persist staged changes.
    /// </example>
    public interface IUnitOfWork : ICommiteable, IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance has active transaction.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has active transaction; otherwise, <c>false</c>.
        /// </value>
        bool HasActiveTransaction { get; }

        /// <summary>
        /// Registers a persistence participant for subsequent commits and transaction enlistment.
        /// </summary>
        /// <param name="commiteable">
        /// The participant to register. A participant already contained according to equality is not added again.
        /// </param>
        void RegisterCommiteable(ICommiteable commiteable);

        /// <summary>
        /// Re-enlists all registered participants in the current active transaction.
        /// </summary>
        /// <returns>The current active transaction.</returns>
        /// <exception cref="InvalidOperationException">There is no active transaction.</exception>
        ITransaction UseCurrentTransaction();

        /// <summary>
        /// Enlists all registered participants in an externally supplied transaction.
        /// </summary>
        /// <param name="transaction">The transaction in which to enlist the participants.</param>
        void UseTransaction(ITransaction transaction);

        /// <summary>
        /// Associates a command with the current transaction when one is active.
        /// </summary>
        /// <param name="command">The command to enlist. It remains unchanged when no transaction is active.</param>
        void UseTransaction(IDbCommand command);
    }
}
