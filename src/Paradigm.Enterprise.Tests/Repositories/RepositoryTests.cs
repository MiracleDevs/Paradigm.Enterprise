using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Tests.Repositories;

[TestClass]
[ExcludeFromCodeCoverage]
public class RepositoryTests
{
    // Sample entity for testing
    public class TestEntity : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public new bool IsNew() => Id == 0;
    }

    // Test DbContext
    public class TestDbContext : DbContextBase
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestEntity>().HasKey(e => e.Id);
        }
    }

    // Test Repository
    public class TestRepository : EditRepositoryBase<TestEntity, TestDbContext>
    {
        public TestRepository(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override DbSet<TestEntity> GetDbSet() => EntityContext.TestEntities;

        protected override IQueryable<TestEntity> AsQueryable() => GetDbSet().AsQueryable();

        protected override Func<FilterTextPaginatedParameters, Task<(PaginationInfo, List<TestEntity>)>> GetSearchPaginatedFunction(
            FilterTextPaginatedParameters parameters) 
        {
            return async (parameters) =>
            {
                var query = AsQueryable();
                
                if (!string.IsNullOrEmpty(parameters.FilterText))
                    query = query.Where(e => e.Name.Contains(parameters.FilterText));

                var totalCount = await query.CountAsync();
                
                // Apply pagination
                var pageSize = parameters.PageSize ?? 10;
                var pageNumber = parameters.PageNumber ?? 1;
                
                query = query.Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize);
                    
                var pageInfo = new PaginationInfo
                {
                    ItemsCount = totalCount,
                    PageNumber = pageNumber,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
                
                return (pageInfo, await query.ToListAsync());
            };
        }
    }

    private ServiceProvider? _serviceProvider;
    private Mock<IUnitOfWork>? _mockUnitOfWork;
    private TestRepository? _repository;
    private DbContextOptions<TestDbContext>? _options;

    [TestInitialize]
    public void Initialize()
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _mockUnitOfWork = new Mock<IUnitOfWork>();
        
        // Set up the UnitOfWork to commit changes when CommitChangesAsync is called
        _mockUnitOfWork.Setup(m => m.CommitChangesAsync()).Returns(async () => {
            using var context = new TestDbContext(_options);
            await context.SaveChangesAsync();
            return true;
        });
        
        var services = new ServiceCollection();
        services.AddSingleton(_mockUnitOfWork.Object);
        services.AddSingleton<TestDbContext>(_ => new TestDbContext(_options));
        _serviceProvider = services.BuildServiceProvider();
        
        _repository = new TestRepository(_serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [TestMethod]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        
        // Act
        var result = await _repository!.AddAsync(entity);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Entity", result.Name);
        _mockUnitOfWork!.Verify(m => m.RegisterCommiteable(It.IsAny<TestDbContext>()), Times.Once);
    }

    [TestMethod]
    public async Task GetByIdAsync_ShouldReturnEntity()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var entity = new TestEntity { Name = "Test Entity" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Act
        var result = await _repository!.GetByIdAsync(entity.Id);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Entity", result.Name);
    }

    [TestMethod]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        context.TestEntities.Add(new TestEntity { Name = "Entity 1" });
        context.TestEntities.Add(new TestEntity { Name = "Entity 2" });
        await context.SaveChangesAsync();
        
        // Act
        var results = await _repository!.GetAllAsync();
        
        // Assert
        Assert.IsNotNull(results);
        Assert.AreEqual(2, results.Count());
    }

    [TestMethod]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var entity = new TestEntity { Name = "Test Entity" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Get the ID from the saved entity
        var entityId = entity.Id;
        
        // Update the entity
        entity.Name = "Updated Entity";
        
        // Act
        var result = await _repository!.UpdateAsync(entity);
        await context.SaveChangesAsync(); // Save changes directly
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Updated Entity", result.Name);
        
        // Verify in database by fetching from the context again
        context.ChangeTracker.Clear(); // Clear tracked entities
        var updated = await context.TestEntities.FindAsync(entityId);
        Assert.IsNotNull(updated);
        Assert.AreEqual("Updated Entity", updated.Name);
    }

    [TestMethod]
    public async Task DeleteAsync_ShouldRemoveEntity()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var entity = new TestEntity { Name = "Test Entity" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        
        // Get the ID from the saved entity
        var entityId = entity.Id;
        
        // Act
        await _repository!.DeleteAsync(entity);
        await context.SaveChangesAsync(); // Save changes directly
        
        // Assert
        context.ChangeTracker.Clear(); // Clear tracked entities
        var deleted = await context.TestEntities.FindAsync(entityId);
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task SearchPaginatedAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        for (int i = 1; i <= 20; i++)
        {
            context.TestEntities.Add(new TestEntity { Name = $"Entity {i}" });
        }
        await context.SaveChangesAsync();

        var parameters = new FilterTextPaginatedParameters
        {
            PageNumber = 2,
            PageSize = 5
        };
        
        // Act
        var result = await _repository!.SearchPaginatedAsync(parameters);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Results.Count());
        Assert.AreEqual(20, result.PageInfo.ItemsCount);
        Assert.AreEqual(4, result.PageInfo.TotalPages);
        Assert.AreEqual(2, result.PageInfo.PageNumber);
    }
} 