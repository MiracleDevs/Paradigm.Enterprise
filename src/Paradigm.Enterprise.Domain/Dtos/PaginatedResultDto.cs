namespace Paradigm.Enterprise.Domain.Dtos
{
    /// <summary>
    /// Combines one page of results with metadata describing that page.
    /// </summary>
    /// <typeparam name="T">The type of item returned by the search.</typeparam>
    public class PaginatedResultDto<T>
    {
        #region Properties 

        /// <summary>
        /// Gets metadata for the returned page.
        /// </summary>
        /// <value>
        /// The page information.
        /// </value>
        public PaginationInfo PageInfo { get; }

        /// <summary>
        /// Gets the items in the returned page.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public IEnumerable<T> Results { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a paginated result.
        /// </summary>
        /// <param name="pageInfo">Metadata describing the returned page.</param>
        /// <param name="results">The page items. A <see langword="null"/> value is normalized to an empty sequence.</param>
        public PaginatedResultDto(PaginationInfo pageInfo, IEnumerable<T> results)
        {
            PageInfo = pageInfo;
            Results = results ?? Enumerable.Empty<T>();
        }

        #endregion
    }
}
