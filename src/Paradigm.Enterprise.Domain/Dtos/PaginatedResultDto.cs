namespace Paradigm.Enterprise.Domain.Dtos
{
    public class PaginatedResultDto<T>
    {
        #region Properties 

        /// <summary>
        /// Gets the page information.
        /// </summary>
        /// <value>
        /// The page information.
        /// </value>
        public PaginationInfo PageInfo { get; }

        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public IEnumerable<T> Results { get; }

        #endregion

        #region Constructor

        public PaginatedResultDto(PaginationInfo pageInfo, IEnumerable<T> results)
        {
            PageInfo = pageInfo;
            Results = results ?? Enumerable.Empty<T>();
        }

        #endregion
    }
}