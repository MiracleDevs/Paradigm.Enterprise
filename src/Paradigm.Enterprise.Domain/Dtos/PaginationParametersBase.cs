namespace Paradigm.Enterprise.Domain.Dtos
{
    public abstract class PaginationParametersBase
    {
        /// <summary>
        /// The default page size
        /// </summary>
        public const int DefaultPageSize = 10;

        /// <summary>
        /// Gets or sets the size of the page.
        /// </summary>
        /// <value>
        /// The size of the page.
        /// </value>
        public int? PageSize { get; set; } = DefaultPageSize;

        /// <summary>
        /// Gets or sets the sort by.
        /// </summary>
        /// <value>
        /// The sort by.
        /// </value>
        public string? SortBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        /// <value>
        /// The sort direction.
        /// </value>
        public string? SortDirection { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the page number.
        /// </summary>
        /// <value>
        /// The page number.
        /// </value>
        public int? PageNumber { get; set; } = 1;
    }
}