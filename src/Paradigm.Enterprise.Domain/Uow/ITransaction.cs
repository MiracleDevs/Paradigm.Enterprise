using System.Data;

namespace Paradigm.Enterprise.Domain.Uow
{
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
        /// Commits this instance.
        /// </summary>
        void Commit();

        /// <summary>
        /// Rollbacks this instance.
        /// </summary>
        void Rollback();

        /// <summary>
        /// Adds the commiteable.
        /// </summary>
        /// <param name="commiteable">The commiteable.</param>
        void AddCommiteable(ICommiteable commiteable);

        /// <summary>
        /// Adds the command.
        /// </summary>
        /// <param name="command">The command.</param>
        void AddCommand(IDbCommand command);
    }
}