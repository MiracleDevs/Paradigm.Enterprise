using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Tests.Providers;

[TestClass]
[ExcludeFromCodeCoverage]
public class ReadProviderBaseTests
{
    // Sample entity for testing
    public class TestEntity : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public new bool IsNew() => Id == 0;
    }

    // Repository interface
    public interface ITestRepository : IReadRepository<TestEntity> { }

    // Provider interface
    public interface ITestProvider : IReadProvider<TestEntity> { }

    // Test provider implementation
    public class TestProvider : ReadProviderBase<IEntity, TestEntity, ITestRepository>, ITestProvider
    {
        public TestProvider(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    private ServiceProvider? _serviceProvider;
    private Mock<ITestRepository>? _mockRepository;
    private TestProvider? _provider;

    [TestInitialize]
    public void Initialize()
    {
        _mockRepository = new Mock<ITestRepository>();
        
        var services = new ServiceCollection();
        services.AddSingleton(_mockRepository.Object);
        _serviceProvider = services.BuildServiceProvider();
        
        _provider = new TestProvider(_serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldCallRepository()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        
        // Act
        var result = await _provider!.GetByIdAsync(1);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Entity", result.Name);
        _mockRepository.Verify(m => m.GetByIdAsync(1), Times.Once);
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1" },
            new() { Name = "Entity 2" }
        };
        _mockRepository!.Setup(m => m.GetAllAsync()).ReturnsAsync(entities);
        
        // Act
        var results = await _provider!.GetAllAsync();
        
        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count());
        _mockRepository.Verify(m => m.GetAllAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SearchPaginatedAsync_ShouldCallRepository()
    {
        // Arrange
        var parameters = new FilterTextPaginatedParameters
        {
            PageNumber = 1,
            PageSize = 10
        };
        
        var entities = new List<TestEntity>
        {
            new() { Name = "Entity 1" },
            new() { Name = "Entity 2" }
        };
        
        var pageInfo = new PaginationInfo
        {
            ItemsCount = 2,
            PageNumber = 1,
            TotalPages = 1
        };
        
        var result = new PaginatedResultDto<TestEntity>(pageInfo, entities);
        
        _mockRepository!.Setup(m => m.SearchPaginatedAsync(It.IsAny<FilterTextPaginatedParameters>())).ReturnsAsync(result);
        
        // Act
        var paginatedResult = await _provider!.SearchPaginatedAsync(parameters);
        
        // Assert
        Assert.IsNotNull(paginatedResult);
        Assert.AreEqual(2, paginatedResult.Results.Count());
        Assert.AreEqual(1, paginatedResult.PageInfo.PageNumber);
        Assert.AreEqual(1, paginatedResult.PageInfo.TotalPages);
        Assert.AreEqual(2, paginatedResult.PageInfo.ItemsCount);
        _mockRepository.Verify(m => m.SearchPaginatedAsync(parameters), Times.Once);
    }
} 