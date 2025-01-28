namespace Paradigm.Enterprise.Domain.Dtos
{
    public class FilterTextPaginatedParameters : PaginationParametersBase
    {
        public string FilterText { get; set; } = string.Empty;

        public bool? IsActive { get; set; }
    }
}