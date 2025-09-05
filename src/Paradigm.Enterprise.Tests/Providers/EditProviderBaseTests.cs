using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Providers.Exceptions;

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

    // Extend TestProvider to test lifecycle hooks
    public class TestProviderWithHooks : EditProviderBase<IEntity, TestEntity, TestView, ITestRepository, ITestViewRepository>, ITestProvider
    {
        public bool BeforeAddViewCalled { get; private set; }
        public bool BeforeAddEntityCalled { get; private set; }
        public bool AfterAddEntityCalled { get; private set; }
        public bool BeforeUpdateViewCalled { get; private set; }
        public bool BeforeUpdateEntityCalled { get; private set; }
        public bool AfterUpdateEntityCalled { get; private set; }
        public bool BeforeSaveViewCalled { get; private set; }
        public bool BeforeSaveEntityCalled { get; private set; }
        public bool AfterSaveEntityCalled { get; private set; }
        public bool BeforeDeleteEntityCalled { get; private set; }
        public bool AfterDeleteEntityCalled { get; private set; }

        public TestProviderWithHooks(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override Task BeforeAddAsync(TestView view)
        {
            BeforeAddViewCalled = true;
            return base.BeforeAddAsync(view);
        }

        protected override Task BeforeAddAsync(TestEntity entity)
        {
            BeforeAddEntityCalled = true;
            return base.BeforeAddAsync(entity);
        }

        protected override Task AfterAddAsync(TestEntity entity)
        {
            AfterAddEntityCalled = true;
            return base.AfterAddAsync(entity);
        }

        protected override Task BeforeUpdateAsync(TestView view)
        {
            BeforeUpdateViewCalled = true;
            return base.BeforeUpdateAsync(view);
        }

        protected override Task BeforeUpdateAsync(TestEntity entity)
        {
            BeforeUpdateEntityCalled = true;
            return base.BeforeUpdateAsync(entity);
        }

        protected override Task AfterUpdateAsync(TestEntity entity)
        {
            AfterUpdateEntityCalled = true;
            return base.AfterUpdateAsync(entity);
        }

        protected override Task BeforeSaveAsync(TestView view)
        {
            BeforeSaveViewCalled = true;
            return base.BeforeSaveAsync(view);
        }

        protected override Task BeforeSaveAsync(TestEntity entity)
        {
            BeforeSaveEntityCalled = true;
            return base.BeforeSaveAsync(entity);
        }

        protected override Task AfterSaveAsync(TestEntity entity)
        {
            AfterSaveEntityCalled = true;
            return base.AfterSaveAsync(entity);
        }

        protected override Task BeforeDeleteAsync(TestEntity entity)
        {
            BeforeDeleteEntityCalled = true;
            return base.BeforeDeleteAsync(entity);
        }

        protected override Task AfterDeleteAsync(TestEntity entity)
        {
            AfterDeleteEntityCalled = true;
            return base.AfterDeleteAsync(entity);
        }
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

    [TestMethod]
    public async Task AddAsync_WithMultipleViews_ShouldCallRepositoryForEach()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        var views = new List<TestView>
        {
            new TestView { Name = "Test View 1" },
            new TestView { Name = "Test View 2" }
        };

        _mockRepository!.Setup(m => m.AddAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);

        // Act
        var results = await _provider!.AddAsync(views);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count());
        _mockRepository.Verify(m => m.AddAsync(It.IsAny<TestEntity>()), Times.Exactly(2));
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateAsync_WithMultipleViews_ShouldCallRepositoryForEach()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var views = new List<TestView>
        {
            new TestView { Id = 1, Name = "Test View 1" },
            new TestView { Id = 2, Name = "Test View 2" }
        };

        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.UpdateAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);

        // Act
        var results = await _provider!.UpdateAsync(views);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count());
        _mockRepository.Verify(m => m.UpdateAsync(It.IsAny<TestEntity>()), Times.Exactly(2));
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SaveAsync_WithMultipleViews_ShouldHandleMixedNewAndExisting()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var views = new List<TestView>
        {
            new TestView { Name = "New View" }, // New entity (Id = 0)
            new TestView { Id = 1, Name = "Existing View" } // Existing entity
        };

        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.AddAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.UpdateAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);

        // Act
        var results = await _provider!.SaveAsync(views);

        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count());
        _mockRepository.Verify(m => m.AddAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockRepository.Verify(m => m.UpdateAsync(It.IsAny<TestEntity>()), Times.Once);
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_WithMultipleIds_ShouldCallRepositoryForEach()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var ids = new List<int> { 1, 2 };

        _mockRepository!.Setup(m => m.GetByIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new List<TestEntity> { entity, entity });

        // Act
        await _provider!.DeleteAsync(ids);

        // Assert
        _mockRepository.Verify(m => m.DeleteAsync(It.IsAny<TestEntity>()), Times.Exactly(2));
        _mockUnitOfWork!.Verify(m => m.CommitChangesAsync(), Times.Once);
    }

    [TestMethod]
    [ExpectedException(typeof(NotFoundException))]
    public async Task UpdateAsync_WhenEntityNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var view = new TestView { Id = 1, Name = "Test View" };

        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((TestEntity)null!);

        // Act - Should throw NotFoundException
        await _provider!.UpdateAsync(view);
    }

    [TestMethod]
    public async Task AddAsync_ShouldCallLifecycleHooks()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        var view = new TestView { Name = "Test View" };

        _mockRepository!.Setup(m => m.AddAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);

        var providerWithHooks = new TestProviderWithHooks(_serviceProvider!);

        // Act
        await providerWithHooks.AddAsync(view);

        // Assert
        Assert.IsTrue(providerWithHooks.BeforeAddViewCalled, "BeforeAddAsync(view) should be called");
        Assert.IsTrue(providerWithHooks.BeforeSaveViewCalled, "BeforeSaveAsync(view) should be called");
        Assert.IsTrue(providerWithHooks.BeforeAddEntityCalled, "BeforeAddAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.BeforeSaveEntityCalled, "BeforeSaveAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.AfterSaveEntityCalled, "AfterSaveAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.AfterAddEntityCalled, "AfterAddAsync(entity) should be called");
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldCallLifecycleHooks()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };
        var view = new TestView { Id = 1, Name = "Test View" };

        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);
        _mockRepository!.Setup(m => m.UpdateAsync(It.IsAny<TestEntity>())).ReturnsAsync(entity);
        _mockViewRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(view);

        var providerWithHooks = new TestProviderWithHooks(_serviceProvider!);

        // Act
        await providerWithHooks.UpdateAsync(view);

        // Assert
        Assert.IsTrue(providerWithHooks.BeforeUpdateViewCalled, "BeforeUpdateAsync(view) should be called");
        Assert.IsTrue(providerWithHooks.BeforeSaveViewCalled, "BeforeSaveAsync(view) should be called");
        Assert.IsTrue(providerWithHooks.BeforeUpdateEntityCalled, "BeforeUpdateAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.BeforeSaveEntityCalled, "BeforeSaveAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.AfterSaveEntityCalled, "AfterSaveAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.AfterUpdateEntityCalled, "AfterUpdateAsync(entity) should be called");
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldCallLifecycleHooks()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test Entity" };

        _mockRepository!.Setup(m => m.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(entity);

        var providerWithHooks = new TestProviderWithHooks(_serviceProvider!);

        // Act
        await providerWithHooks.DeleteAsync(1);

        // Assert
        Assert.IsTrue(providerWithHooks.BeforeDeleteEntityCalled, "BeforeDeleteAsync(entity) should be called");
        Assert.IsTrue(providerWithHooks.AfterDeleteEntityCalled, "AfterDeleteAsync(entity) should be called");
    }
}