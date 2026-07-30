namespace Paradigm.Enterprise.Domain.Uow
{
    /// <summary>
    /// Defines a persistence participant whose staged changes can be committed and whose connection can begin a transaction.
    /// </summary>
    public interface ICommiteable
    {
        /// <summary>
        /// Persists the participant's currently staged changes.
        /// </summary>
        /// <returns>A task that completes when persistence finishes.</returns>
        Task CommitChangesAsync();

        /// <summary>
        /// Begins a transaction owned by this persistence participant.
        /// </summary>
        /// <returns>The active transaction wrapper.</returns>
        ITransaction CreateTransaction();
    }
}
