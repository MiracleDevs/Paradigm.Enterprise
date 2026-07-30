namespace Paradigm.Enterprise.Domain.Dtos
{
    /// <summary>
    /// Provides common page and sort inputs for repository searches.
    /// </summary>
    public abstract class PaginationParametersBase
    {
        /// <summary>
        /// The number of items requested per page when no other size is supplied.
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// Gets or sets the requested number of items per page.
        /// </summary>
        /// <value>
        /// The size of the page.
        /// </value>
        public int? PageSize { get; set; } = DefaultPageSize;

        /// <summary>
        /// Gets or sets the implementation-defined member used for sorting.
        /// </summary>
        /// <value>
        /// The sort by.
        /// </value>
        public string? SortBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the implementation-defined sort direction.
        /// </summary>
        /// <value>
        /// The sort direction.
        /// </value>
        public string? SortDirection { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the requested one-based page number.
        /// </summary>
        /// <value>
        /// The page number.
        /// </value>
        public int? PageNumber { get; set; } = 1;
    }
}
