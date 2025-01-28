using System.Data;

namespace Paradigm.Enterprise.Domain.Uow
{
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
        /// Registers the commiteable.
        /// </summary>
        /// <param name="commiteable">The commiteable.</param>
        void RegisterCommiteable(ICommiteable commiteable);

        /// <summary>
        /// Uses the current transaction.
        /// </summary>
        ITransaction UseCurrentTransaction();

        /// <summary>
        /// Uses the transaction.
        /// </summary>
        /// <param name="transaction">The transaction.</param>
        void UseTransaction(ITransaction transaction);

        /// <summary>
        /// Uses the transaction.
        /// </summary>
        /// <param name="command">The command.</param>
        void UseTransaction(IDbCommand command);
    }
}