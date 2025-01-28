namespace Paradigm.Enterprise.Domain.Dtos
{
    public class PaginationInfo
    {
        /// <summary>
        /// Gets or sets the items count.
        /// </summary>
        /// <value>
        /// The items count.
        /// </value>
        public int ItemsCount { get; set; }

        /// <summary>
        /// Gets or sets the total pages.
        /// </summary>
        /// <value>
        /// The total pages.
        /// </value>
        public int TotalPages { get; set; }

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        /// <value>
        /// The page number.
        /// </value>
        public int PageNumber { get; set; }
    }
}