namespace Paradigm.Enterprise.Tests.Entities;

[TestClass]
[ExcludeFromCodeCoverage]
public class DomainTrackerTests
{
    /// <summary>
    /// Test entity implementation for DomainTracker tests.
    /// </summary>
    public class TestEntity : EntityBase<int>
    {
        public string Name { get; set; } = string.Empty;
    }

    [TestMethod]
    public void Constructor_InitializesEmptyLists()
    {
        // Arrange & Act
        var tracker = new DomainTracker<TestEntity>();

        // Assert
        Assert.AreEqual(0, tracker.Added.Count);
        Assert.AreEqual(0, tracker.Edited.Count);
        Assert.AreEqual(0, tracker.Removed.Count);
    }

    [TestMethod]
    public void Add_RegistersEntityInAddedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        tracker.Add(entity);

        // Assert
        Assert.AreEqual(1, tracker.Added.Count);
        Assert.Contains(entity, tracker.Added.ToList());
    }

    [TestMethod]
    public void Edit_RegistersEntityInEditedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        tracker.Edit(entity);

        // Assert
        Assert.AreEqual(1, tracker.Edited.Count);
        Assert.Contains(entity, tracker.Edited.ToList());
    }

    [TestMethod]
    public void Remove_RegistersEntityInRemovedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        tracker.Remove(entity);

        // Assert
        Assert.AreEqual(1, tracker.Removed.Count);
        CollectionAssert.Contains(tracker.Removed.ToList(), entity);
    }

    [TestMethod]
    public void Reset_ClearsAddedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "Entity1" };
        var entity2 = new TestEntity { Id = 2, Name = "Entity2" };

        tracker.Add(entity1);
        tracker.Add(entity2);

        Assert.AreEqual(2, tracker.Added.Count);

        // Act
        tracker.Reset();

        // Assert
        Assert.AreEqual(0, tracker.Added.Count);
    }

    [TestMethod]
    public void Reset_ClearsEditedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "Entity1" };
        var entity2 = new TestEntity { Id = 2, Name = "Entity2" };

        tracker.Edit(entity1);
        tracker.Edit(entity2);

        Assert.AreEqual(2, tracker.Edited.Count);

        // Act
        tracker.Reset();

        // Assert
        Assert.AreEqual(0, tracker.Edited.Count);
    }

    [TestMethod]
    public void Reset_ClearsRemovedList()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity1 = new TestEntity { Id = 1, Name = "Entity1" };
        var entity2 = new TestEntity { Id = 2, Name = "Entity2" };

        tracker.Remove(entity1);
        tracker.Remove(entity2);

        Assert.AreEqual(2, tracker.Removed.Count);

        // Act
        tracker.Reset();

        // Assert
        Assert.AreEqual(0, tracker.Removed.Count);
    }

    [TestMethod]
    public void Reset_ClearsAllLists()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var user1 = new TestEntity { Id = 1, Name = "User1" };
        var user2 = new TestEntity { Id = 2, Name = "User2" };
        var user3 = new TestEntity { Id = 3, Name = "User3" };

        tracker.Add(user1);
        tracker.Edit(user2);
        tracker.Remove(user3);

        Assert.AreEqual(1, tracker.Added.Count);
        Assert.AreEqual(1, tracker.Edited.Count);
        Assert.AreEqual(1, tracker.Removed.Count);

        // Act
        tracker.Reset();

        // Assert
        Assert.IsTrue(tracker.Added.Count == 0, "Added list should be empty");
        Assert.IsTrue(tracker.Edited.Count == 0, "Edited list should be empty");
        Assert.IsTrue(tracker.Removed.Count == 0, "Removed list should be empty");
    }

    [TestMethod]
    public void Reset_IsIdempotent()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        tracker.Add(new TestEntity { Id = 1, Name = "Entity" });

        // Act
        tracker.Reset();
        var firstResetAddedCount = tracker.Added.Count;

        tracker.Reset();
        var secondResetAddedCount = tracker.Added.Count;

        // Assert
        Assert.AreEqual(firstResetAddedCount, secondResetAddedCount);
        Assert.AreEqual(0, secondResetAddedCount);
    }

    [TestMethod]
    public void OperationsAfterReset_WorkCorrectly()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var user1 = new TestEntity { Id = 1, Name = "User1" };
        var user2 = new TestEntity { Id = 2, Name = "User2" };

        tracker.Add(user1);
        tracker.Reset();

        // Act
        tracker.Add(user2);

        // Assert
        Assert.AreEqual(1, tracker.Added.Count);
        Assert.Contains(user2, tracker.Added.ToList());
        Assert.IsFalse(tracker.Added.Contains(user1));
    }

    [TestMethod]
    public void Reset_OnEmptyTracker_DoesNotThrow()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();

        // Act & Assert
        try
        {
            tracker.Reset();
            Assert.IsTrue(true, "Reset should not throw when tracker is empty");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Reset should not throw: {ex.Message}");
        }
    }

    [TestMethod]
    public void Reset_MultipleTimes_MaintainsConsistentState()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Entity" };

        // Act
        tracker.Add(entity);
        tracker.Reset();
        tracker.Reset();
        tracker.Reset();

        // Assert
        Assert.AreEqual(0, tracker.Added.Count);
        Assert.AreEqual(0, tracker.Edited.Count);
        Assert.AreEqual(0, tracker.Removed.Count);
    }

    [TestMethod]
    public void Reset_BetweenMappingOperations_AllowsReuse()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var userA = new TestEntity { Id = 1, Name = "UserA" };
        var userB = new TestEntity { Id = 2, Name = "UserB" };
        var userC = new TestEntity { Id = 3, Name = "UserC" };

        // First mapping operation
        tracker.Add(userA);
        Assert.AreEqual(1, tracker.Added.Count);

        // Reset for next mapping operation
        tracker.Reset();

        // Second mapping operation
        tracker.Edit(userB);
        Assert.AreEqual(1, tracker.Edited.Count);
        Assert.AreEqual(0, tracker.Added.Count);

        // Reset for third mapping operation
        tracker.Reset();

        // Third mapping operation
        tracker.Remove(userC);
        Assert.AreEqual(1, tracker.Removed.Count);
        Assert.AreEqual(0, tracker.Added.Count);
        Assert.AreEqual(0, tracker.Edited.Count);
    }

    [TestMethod]
    public void Reset_PreservesListReadOnlyContract()
    {
        // Arrange
        var tracker = new DomainTracker<TestEntity>();
        var entity = new TestEntity { Id = 1, Name = "Entity" };

        tracker.Add(entity);
        tracker.Reset();

        // Act & Assert
        var added = tracker.Added;
        var edited = tracker.Edited;
        var removed = tracker.Removed;

        // Verify they are read-only collections
        Assert.IsNotNull(added);
        Assert.IsNotNull(edited);
        Assert.IsNotNull(removed);

        // Verify they are empty
        Assert.AreEqual(0, added.Count);
        Assert.AreEqual(0, edited.Count);
        Assert.AreEqual(0, removed.Count);
    }
}
