using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Tests.Dtos;

[TestClass]
[ExcludeFromCodeCoverage]
public class PaginatedResultDtoTests
{
    [TestMethod]
    public void Constructor_WithValidData_ShouldInitializeProperties()
    {
        // Arrange
        var pageInfo = new PaginationInfo
        {
            ItemsCount = 100,
            PageNumber = 2,
            TotalPages = 10
        };
        var results = new List<string> { "Item1", "Item2", "Item3" };

        // Act
        var paginatedResult = new PaginatedResultDto<string>(pageInfo, results);

        // Assert
        Assert.IsNotNull(paginatedResult);
        Assert.AreEqual(pageInfo, paginatedResult.PageInfo);
        Assert.AreEqual(results, paginatedResult.Results);
        Assert.AreEqual(100, paginatedResult.PageInfo.ItemsCount);
        Assert.AreEqual(2, paginatedResult.PageInfo.PageNumber);
        Assert.AreEqual(10, paginatedResult.PageInfo.TotalPages);
        Assert.AreEqual(3, paginatedResult.Results.Count());
    }

    [TestMethod]
    public void Constructor_WithNullResults_ShouldUseEmptyCollection()
    {
        // Arrange
        var pageInfo = new PaginationInfo
        {
            ItemsCount = 0,
            PageNumber = 1,
            TotalPages = 0
        };

        // Act
        var paginatedResult = new PaginatedResultDto<string>(pageInfo, null!);

        // Assert
        Assert.IsNotNull(paginatedResult);
        Assert.IsNotNull(paginatedResult.Results);
        Assert.AreEqual(0, paginatedResult.Results.Count());
    }

    [TestMethod]
    public void PageInfo_Properties_ShouldWorkCorrectly()
    {
        // Arrange
        var pageInfo = new PaginationInfo();

        // Act & Assert
        pageInfo.ItemsCount = 200;
        Assert.AreEqual(200, pageInfo.ItemsCount);

        pageInfo.PageNumber = 3;
        Assert.AreEqual(3, pageInfo.PageNumber);

        pageInfo.TotalPages = 20;
        Assert.AreEqual(20, pageInfo.TotalPages);
    }

    [TestMethod]
    public void FilterTextPaginatedParameters_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var parameters = new FilterTextPaginatedParameters();

        // Assert
        Assert.AreEqual(string.Empty, parameters.FilterText);
        Assert.IsNull(parameters.IsActive);
        Assert.AreEqual(1, parameters.PageNumber);
        Assert.AreEqual(10, parameters.PageSize);
        Assert.AreEqual(string.Empty, parameters.SortBy);
        Assert.AreEqual(string.Empty, parameters.SortDirection);
    }

    [TestMethod]
    public void FilterTextPaginatedParameters_CustomValues_ShouldBeSet()
    {
        // Arrange
        var parameters = new FilterTextPaginatedParameters
        {
            FilterText = "search term",
            IsActive = true,
            PageNumber = 3,
            PageSize = 25,
            SortBy = "Name",
            SortDirection = "DESC"
        };

        // Assert
        Assert.AreEqual("search term", parameters.FilterText);
        Assert.IsTrue(parameters.IsActive!.Value);
        Assert.AreEqual(3, parameters.PageNumber);
        Assert.AreEqual(25, parameters.PageSize);
        Assert.AreEqual("Name", parameters.SortBy);
        Assert.AreEqual("DESC", parameters.SortDirection);
    }

    [TestMethod]
    public void PaginationParametersBase_DefaultConstants_ShouldBeCorrect()
    {
        // Assert
        Assert.AreEqual(10, PaginationParametersBase.DefaultPageSize);
    }
}