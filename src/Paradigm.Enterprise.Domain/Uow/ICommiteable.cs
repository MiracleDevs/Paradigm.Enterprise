namespace Paradigm.Enterprise.Domain.Uow
{
    public interface ICommiteable
    {
        /// <summary>
        /// Commits the changes.
        /// </summary>
        /// <returns></returns>
        Task CommitChangesAsync();

        /// <summary>
        /// Creates the transaction.
        /// </summary>
        /// <returns></returns>
        ITransaction CreateTransaction();
    }
}