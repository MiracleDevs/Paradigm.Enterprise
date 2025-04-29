# Paradigm.Enterprise.Tests

The Tests project provides a collection of unit and integration tests for the Paradigm.Enterprise framework. It ensures the functionality, reliability, and performance of the framework components through automated testing.

## Key Components

### Test Fixtures

The project includes various test fixtures to set up test environments:

- **DataContextFixture** - Sets up test database contexts
- **RepositoryFixture** - Prepares repository instances for testing
- **ProviderFixture** - Initializes providers with mock dependencies

### Unit Tests

Unit tests for core framework components:

- **EntityTests** - Tests for domain entities and their behavior
- **RepositoryTests** - Tests for repository implementations
- **ProviderTests** - Tests for provider implementations
- **ServiceTests** - Tests for service implementations

### Integration Tests

Integration tests that verify the interaction between components:

- **DataAccessTests** - Tests for data access and persistence
- **WebApiTests** - Tests for API controllers and endpoints
- **End-to-EndTests** - Tests that cover complete application flows

### Mocks and Test Doubles

The project includes mock implementations for testing:

- **MockRepository** - In-memory repository implementation
- **MockDataContext** - In-memory database context
- **MockProvider** - Simplified provider for testing
- **TestEntities** - Sample entity implementations for tests

## Test Patterns

The Tests project follows these testing patterns:

1. **Arrange-Act-Assert (AAA)** - Clear separation between test setup, execution, and verification
2. **Test Data Builders** - Fluent builders for creating test data
3. **Object Mother Pattern** - Factory methods for common test objects
4. **Test Categories** - Tests are categorized for selective execution

## Usage Example

```csharp
// Example of a repository unit test
[Fact]
public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
{
    // Arrange
    var context = new MockDataContext();
    var entity = new TestProduct { Id = 1, Name = "Test Product" };
    context.Products.Add(entity);
    context.SaveChanges();
    
    var repository = new RepositoryBase<TestProduct>(context);
    
    // Act
    var result = await repository.GetByIdAsync(1);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
    Assert.Equal("Test Product", result.Name);
}

// Example of a provider test with mocks
[Fact]
public async Task CreateAsync_ValidModel_CreatesEntityAndReturnsView()
{
    // Arrange
    var mockRepository = new Mock<IRepository<Product>>();
    var mockUnitOfWork = new Mock<IUnitOfWork>();
    var serviceProvider = new ServiceCollection().BuildServiceProvider();
    
    mockRepository.Setup(r => r.CreateAsync(It.IsAny<Product>()))
                 .ReturnsAsync((Product p) => p);
    mockUnitOfWork.Setup(u => u.CommitAsync())
                 .ReturnsAsync(true);
    
    var provider = new ProductProvider(
        mockRepository.Object,
        mockUnitOfWork.Object,
        serviceProvider);
    
    var model = new ProductEditDto { Name = "New Product", Price = 10.99m };
    
    // Act
    var result = await provider.CreateAsync(model);
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("New Product", result.Name);
    mockRepository.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);
    mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
}
```

## Test Configuration

The Tests project includes configuration for various test frameworks and tools:

- **xUnit** - The primary test framework
- **Moq** - For creating mock objects
- **Shouldly** - For fluent assertions
- **TestHost** - For in-memory hosting of web applications

## Running Tests

Tests can be executed using the following command:

```shell
dotnet test src/Paradigm.Enterprise.Tests
```

To run specific test categories:

```shell
dotnet test src/Paradigm.Enterprise.Tests --filter Category=UnitTest
```

## Continuous Integration

The Tests project is configured for continuous integration, providing:

1. **Automatic Test Execution** - Tests run on each build/pull request
2. **Code Coverage Reports** - Tracks code coverage of the tests
3. **Test Result Reporting** - Generates reports of test results
4. **Performance Metrics** - Monitors test execution performance 