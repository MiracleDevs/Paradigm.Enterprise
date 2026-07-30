namespace Paradigm.Enterprise.Domain.Dtos
{
    /// <summary>
    /// Describes the size and position of a page within a result set.
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// Gets or sets the total number of matching items across all pages.
        /// </summary>
        /// <value>
        /// The items count.
        /// </value>
        public int ItemsCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of available pages.
        /// </summary>
        /// <value>
        /// The total pages.
        /// </value>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the one-based number of the returned page.
        /// </summary>
        /// <value>
        /// The page number.
        /// </value>
        public int PageNumber { get; set; }
    }
}
