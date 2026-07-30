using System.Data;

namespace Paradigm.Enterprise.Domain.Uow
{
    /// <summary>
    /// Coordinates a database transaction across compatible persistence participants and commands.
    /// </summary>
    /// <remarks>
    /// Attached contexts and commands must be able to use the same underlying database transaction
    /// and connection. Implementations are not distributed transaction coordinators.
    /// </remarks>
    public interface ITransaction : IDisposable
    {
        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is active; otherwise, <c>false</c>.
        /// </value>
        bool IsActive { get; }

        /// <summary>
        /// Commits the underlying database transaction and detaches participating contexts.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rolls back the underlying database transaction and detaches participating contexts.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Enlists a compatible persistence participant in this transaction.
        /// </summary>
        /// <param name="commiteable">The participant to enlist.</param>
        void AddCommiteable(ICommiteable commiteable);

        /// <summary>
        /// Associates a database command with this transaction.
        /// </summary>
        /// <param name="command">The command to enlist. Its connection must be compatible with the transaction.</param>
        void AddCommand(IDbCommand command);
    }
}
