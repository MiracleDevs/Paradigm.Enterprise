using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Paradigm.Enterprise.Data.Context;
using Paradigm.Enterprise.Data.Repositories;
using Paradigm.Enterprise.Domain.Dtos;

namespace Paradigm.Enterprise.Tests.Repositories;

[TestClass]
[ExcludeFromCodeCoverage]
public class EditRepositoryBaseTests
{
    // Aggregate root entity for testing
    public class TestAggregateRoot : EntityBase, IEntity
    {
        public string Name { get; set; } = string.Empty;

        public DomainTracker<TestAggregatedChild> ChildTracker { get; set; } = new();
    }

    // Aggregated child entity for testing
    public class TestAggregatedChild : EntityBase, IEntity
    {
        public string Description { get; set; } = string.Empty;

        public int AggregateRootId { get; set; }
    }

    // Test DbContext
    public class TestDbContext : DbContextBase
    {
        public DbSet<TestAggregateRoot> AggregateRoots { get; set; } = null!;

        public DbSet<TestAggregatedChild> AggregatedChildren { get; set; } = null!;

        public TestDbContext(IServiceProvider serviceProvider, DbContextOptions<TestDbContext> options) : base(serviceProvider, options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TestAggregateRoot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Ignore(e => e.ChildTracker);
            });
            modelBuilder.Entity<TestAggregatedChild>().HasKey(e => e.Id);
            modelBuilder.Entity<TestAggregatedChild>()
                .HasOne<TestAggregateRoot>()
                .WithMany()
                .HasForeignKey(c => c.AggregateRootId);
        }
    }

    // Test Repository with RemoveAggregate implementation
    public class TestAggregateRepository : EditRepositoryBase<TestAggregateRoot, TestDbContext>
    {
        public TestAggregateRepository(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override IQueryable<TestAggregateRoot> AsQueryable() => EntityContext.AggregateRoots.AsQueryable();

        protected override void DeleteRemovedAggregates(TestAggregateRoot entity)
        {
            // Use RemoveAggregate to delete removed children
            RemoveAggregate(entity.ChildTracker.Removed);
        }

        protected override Func<PaginationParametersBase, Task<(PaginationInfo, List<TestAggregateRoot>)>> GetSearchPaginatedFunction(PaginationParametersBase parameters)
        {
            return async (parameters) =>
            {
                var query = AsQueryable();
                var totalCount = await query.CountAsync();
                var pageSize = parameters.PageSize ?? 10;
                var pageNumber = parameters.PageNumber ?? 1;

                query = query.Skip((pageNumber - 1) * pageSize).Take(pageSize);

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

    private TestAggregateRepository? _repository;

    private DbContextOptions<TestDbContext>? _options;

    [TestInitialize]
    public void Initialize()
    {
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _mockUnitOfWork = new Mock<IUnitOfWork>();

        // Set up the UnitOfWork to commit changes when CommitChangesAsync is called
        _mockUnitOfWork.Setup(m => m.CommitChangesAsync()).Returns(async () =>
        {
            using var context = new TestDbContext(_serviceProvider!, _options);
            await context.SaveChangesAsync();
            return true;
        });

        var services = new ServiceCollection();
        services.AddSingleton(_mockUnitOfWork.Object);
        services.AddSingleton(serviceProvider => new TestDbContext(serviceProvider, _options));
        _serviceProvider = services.BuildServiceProvider();

        _repository = new TestAggregateRepository(_serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    #region UpdateAsync Integration Tests

    [TestMethod]
    public async Task UpdateAsync_WithSingleRemovedAggregate_DeletesAggregate()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate = new TestAggregateRoot { Name = "Test Aggregate" };
        var child = new TestAggregatedChild { Description = "Child 1", AggregateRootId = 0 };

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.Add(child);
        await context.SaveChangesAsync();

        var childId = child.Id;

        // Mark child as removed
        aggregate.ChildTracker.Remove(child);

        // Act
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var deletedChild = await context.AggregatedChildren.FindAsync(childId);
        Assert.IsNull(deletedChild, "Child should be deleted from database");
    }

    [TestMethod]
    public async Task UpdateAsync_WithMultipleRemovedAggregates_DeletesAllRemovedAggregates()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate = new TestAggregateRoot { Name = "Test Aggregate" };
        var child1 = new TestAggregatedChild { Description = "Child 1", AggregateRootId = 0 };
        var child2 = new TestAggregatedChild { Description = "Child 2", AggregateRootId = 0 };
        var child3 = new TestAggregatedChild { Description = "Child 3", AggregateRootId = 0 };

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.AddRange(child1, child2, child3);
        await context.SaveChangesAsync();

        var child1Id = child1.Id;
        var child2Id = child2.Id;
        var child3Id = child3.Id;

        // Mark child1 and child2 as removed
        aggregate.ChildTracker.Remove(child1);
        aggregate.ChildTracker.Remove(child2);

        // Act
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var deletedChild1 = await context.AggregatedChildren.FindAsync(child1Id);
        var deletedChild2 = await context.AggregatedChildren.FindAsync(child2Id);
        var existingChild3 = await context.AggregatedChildren.FindAsync(child3Id);

        Assert.IsNull(deletedChild1, "Child 1 should be deleted");
        Assert.IsNull(deletedChild2, "Child 2 should be deleted");
        Assert.IsNotNull(existingChild3, "Child 3 should still exist");
    }

    [TestMethod]
    public async Task UpdateAsync_WithNoRemovedAggregates_NoDeletions()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate = new TestAggregateRoot { Name = "Test Aggregate" };
        var child = new TestAggregatedChild { Description = "Child 1", AggregateRootId = 0 };

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.Add(child);
        await context.SaveChangesAsync();

        var childId = child.Id;

        // Act
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var existingChild = await context.AggregatedChildren.FindAsync(childId);
        Assert.IsNotNull(existingChild, "Child should still exist");
    }

    [TestMethod]
    public async Task UpdateAsync_WithEmptyRemovedCollection_NoExceptions()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate = new TestAggregateRoot { Name = "Test Aggregate" };
        var child = new TestAggregatedChild { Description = "Child 1", AggregateRootId = 0 };

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.Add(child);
        await context.SaveChangesAsync();

        var childId = child.Id;

        // Act - Update without marking any children as removed (tracker.Removed will be empty)
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert - Should not throw and child should still exist
        context.ChangeTracker.Clear();
        var existingChild = await context.AggregatedChildren.FindAsync(childId);
        Assert.IsNotNull(existingChild, "Child should still exist when no aggregates are removed");
    }

    [TestMethod]
    public async Task UpdateAsync_MultipleEntities_DeletesAggregatesForEach()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate1 = new TestAggregateRoot { Name = "Aggregate 1" };
        var aggregate2 = new TestAggregateRoot { Name = "Aggregate 2" };
        var child1 = new TestAggregatedChild { Description = "Child 1", AggregateRootId = 0 };
        var child2 = new TestAggregatedChild { Description = "Child 2", AggregateRootId = 0 };

        context.AggregateRoots.AddRange(aggregate1, aggregate2);
        context.AggregatedChildren.AddRange(child1, child2);
        await context.SaveChangesAsync();

        var child1Id = child1.Id;
        var child2Id = child2.Id;

        aggregate1.ChildTracker.Remove(child1);
        aggregate2.ChildTracker.Remove(child2);

        // Act
        await _repository!.UpdateAsync(new[] { aggregate1, aggregate2 });
        await context.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        var deletedChild1 = await context.AggregatedChildren.FindAsync(child1Id);
        var deletedChild2 = await context.AggregatedChildren.FindAsync(child2Id);

        Assert.IsNull(deletedChild1, "Child 1 should be deleted");
        Assert.IsNull(deletedChild2, "Child 2 should be deleted");
    }

    [TestMethod]
    public async Task UpdateAsync_WithLargeCollectionOfRemovedAggregates_DeletesAll()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();
        var aggregate = new TestAggregateRoot { Name = "Test Aggregate" };
        var children = new List<TestAggregatedChild>();

        for (int i = 0; i < 50; i++)
        {
            children.Add(new TestAggregatedChild { Description = $"Child {i}", AggregateRootId = 0 });
        }

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.AddRange(children);
        await context.SaveChangesAsync();

        var childIds = children.Select(c => c.Id).ToList();

        // Mark all children as removed
        foreach (var child in children)
        {
            aggregate.ChildTracker.Remove(child);
        }

        // Act
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert
        context.ChangeTracker.Clear();
        foreach (var childId in childIds)
        {
            var deletedChild = await context.AggregatedChildren.FindAsync(childId);
            Assert.IsNull(deletedChild, $"Child with ID {childId} should be deleted");
        }
    }

    #endregion

    #region End-to-End Integration Tests

    [TestMethod]
    public async Task EndToEnd_AggregateDeletion_CompleteFlow()
    {
        // Arrange
        var context = _serviceProvider!.GetRequiredService<TestDbContext>();

        // Create aggregate root with multiple children
        var aggregate = new TestAggregateRoot { Name = "Sales Order" };
        var child1 = new TestAggregatedChild { Description = "Line 1", AggregateRootId = 0 };
        var child2 = new TestAggregatedChild { Description = "Line 2", AggregateRootId = 0 };
        var child3 = new TestAggregatedChild { Description = "Line 3", AggregateRootId = 0 };

        context.AggregateRoots.Add(aggregate);
        context.AggregatedChildren.AddRange(child1, child2, child3);
        await context.SaveChangesAsync();

        var child1Id = child1.Id;
        var child2Id = child2.Id;
        var child3Id = child3.Id;

        // Verify all children exist
        context.ChangeTracker.Clear();
        Assert.IsNotNull(await context.AggregatedChildren.FindAsync(child1Id));
        Assert.IsNotNull(await context.AggregatedChildren.FindAsync(child2Id));
        Assert.IsNotNull(await context.AggregatedChildren.FindAsync(child3Id));

        // Clear tracker again and reload aggregate from database to get fresh instances
        context.ChangeTracker.Clear();
        var aggregateId = aggregate.Id;
        aggregate = await context.AggregateRoots.FindAsync(aggregateId);
        Assert.IsNotNull(aggregate);

        // Reload children from database to get fresh tracked instances
        child1 = await context.AggregatedChildren.FindAsync(child1Id);
        child2 = await context.AggregatedChildren.FindAsync(child2Id);
        child3 = await context.AggregatedChildren.FindAsync(child3Id);
        Assert.IsNotNull(child1);
        Assert.IsNotNull(child2);
        Assert.IsNotNull(child3);

        // Mark child1 and child2 as removed via tracker
        aggregate!.ChildTracker.Remove(child1!);
        aggregate.ChildTracker.Remove(child2!);

        // Act - Update the aggregate (which should trigger DeleteRemovedAggregates)
        await _repository!.UpdateAsync(aggregate);
        await context.SaveChangesAsync();

        // Assert - Verify removed children are deleted, remaining child still exists
        context.ChangeTracker.Clear();
        var deletedChild1 = await context.AggregatedChildren.FindAsync(child1Id);
        var deletedChild2 = await context.AggregatedChildren.FindAsync(child2Id);
        var existingChild3 = await context.AggregatedChildren.FindAsync(child3Id);

        Assert.IsNull(deletedChild1, "Removed child 1 should be deleted from database");
        Assert.IsNull(deletedChild2, "Removed child 2 should be deleted from database");
        Assert.IsNotNull(existingChild3, "Non-removed child 3 should still exist in database");
        Assert.AreEqual("Line 3", existingChild3!.Description);
    }

    #endregion
}

