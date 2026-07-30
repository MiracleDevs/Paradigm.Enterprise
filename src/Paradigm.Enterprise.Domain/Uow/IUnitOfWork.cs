using System.Data;

namespace Paradigm.Enterprise.Domain.Uow
{
    /// <summary>
    /// Coordinates commits and an optional shared transaction across registered persistence participants.
    /// </summary>
    /// <remarks>
    /// Registration and repository operations only stage work. <see cref="ICommiteable.CommitChangesAsync"/>
    /// invokes every registered participant. Transactions can include only participants and commands
    /// compatible with the transaction created by the first registered participant.
    /// </remarks>
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
        /// <param name="commiteable">The participant to register. Duplicate instances are ignored.</param>
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
