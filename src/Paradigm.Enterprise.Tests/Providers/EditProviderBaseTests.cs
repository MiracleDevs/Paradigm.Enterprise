using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Tests.Providers;

[TestClass]
[ExcludeFromCodeCoverage]
public class EditProviderBaseTests
{
    // Sample entity classes for testing
    public class TestEntity : EntityBase<IEntity, TestEntity, TestView>, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public override TestEntity? MapFrom(IServiceProvider serviceProvider, IEntity model)
        {
            if (model is TestView view)
            {
                Id = view.Id;
                Name = view.Name;
            }
            return this;
        }

        public override TestView MapTo(IServiceProvider serviceProvider)
        {
            return new TestView
            {
                Id = Id,
                Name = Name
            };
        }
    }

    public class TestView : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public new bool IsNew() => Id == 0;
    }

    // Repository interfaces
    public interface ITestRepository : IEditRepository<TestEntity> { }
    public interface ITestViewRepository : IReadRepository<TestView> { }

    // Provider interface
    public interface ITestProvider : IEditProvider<TestView> { }

    // Test provider implementation
    public class TestProvider : EditProviderBase<IEntity, TestEntity, TestView, ITestRepository, ITestViewRepository>, ITestProvider
    {
        public TestProvider(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    private ServiceProvider? _serviceProvider;
    private Mock<ITestRepository>? _mockRepository;
    private Mock<ITestViewRepository>? _mockViewRepository;
    private Mock<IUnitOfWork>? _mockUnitOfWork;
    private TestProvider? _provider;

    [TestInitialize]
    public void Initialize()
    {
        _mockRepository = new Mock<ITestRepository>();
        _mockViewRepository = new Mock<ITestViewRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        var services = new ServiceCollection();
        services.AddSingleton(_mockRepository.Object);
        services.AddSingleton(_mockViewRepository.Object);
        services.AddSingleton(_mockUnitOfWork.Object);
        services.AddTransient<TestEntity>();
        _serviceProvider = services.BuildServiceProvider();
        
        _provider = new TestProvider(_serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_ShouldCallRepository()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        var view = new TestView { Name = "Test View" };
        
        _mockRepository!.Setup(m => m.AddAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);
        
        // Act
        var result = await _provider!.AddAsync(view);
        
        // Assert
        Assert.IsNotNull(result);
        _mockRepository.Verify(m => m.AddAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldCallRepository()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var view = new TestView { Id = 1, Name = "Test View" };
        
        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.UpdateAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);
        
        // Act
        var result = await _provider!.UpdateAsync(view);
        
        // Assert
        Assert.IsNotNull(result);
        _mockRepository.Verify(m => m.UpdateAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldCallRepository()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        
        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        
        // Act
        await _provider!.DeleteAsync(1);
        
        // Assert
        _mockRepository.Verify(m => m.DeleteAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SaveAsync_WithNewEntity_ShouldCallAddAsync()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        var view = new TestView { Name = "Test View" }; // Id = 0 means it's new
        
        _mockRepository!.Setup(m => m.AddAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);
        
        // Act
        var result = await _provider!.SaveAsync(view);
        
        // Assert
        Assert.IsNotNull(result);
        _mockRepository.Verify(m => m.AddAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SaveAsync_WithExistingEntity_ShouldCallUpdateAsync()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var view = new TestView { Id = 1, Name = "Test View" }; // Id != 0 means it exists
        
        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.UpdateAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);
        
        // Act
        var result = await _provider!.SaveAsync(view);
        
        // Assert
        Assert.IsNotNull(result);
        _mockRepository.Verify(m => m.UpdateAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }
} 