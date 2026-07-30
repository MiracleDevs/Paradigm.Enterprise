namespace Paradigm.Enterprise.Domain.Dtos
{
    /// <summary>
    /// Supplies common free-text, active-state, sorting, and paging inputs for a search.
    /// </summary>
    public class FilterTextPaginatedParameters : PaginationParametersBase
    {
        /// <summary>
        /// Gets or sets the text used by the repository's search implementation.
        /// </summary>
        /// <value>An empty string when no text filter is requested.</value>
        public string FilterText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional active-state filter.
        /// </summary>
        /// <value><see langword="null"/> when records of either state should be included.</value>
        public bool? IsActive { get; set; }
    }
}
