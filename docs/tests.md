# 1. Paradigm.Enterprise.Tests

The Tests project provides a collection of unit and integration tests for the Paradigm.Enterprise framework. It ensures the functionality, reliability, and performance of the framework components through automated testing.

## 1.1. Key Components

### 1.1.1. Test Fixtures

The project includes various test fixtures to set up test environments for different test scenarios:

- **ProviderTestBase** - Base class for provider testing
- **EntityTestBase** - Base class for entity testing
- **ServiceTestBase** - Base class for service testing

### 1.1.2. Unit Tests

Unit tests for core framework components:

- **EntityTests** - Tests for domain entities and their behavior (e.g., ValidatableEntityTests)
- **RepositoryTests** - Tests for repository implementations
- **ProviderTests** - Tests for provider implementations (e.g., ReadProviderBaseTests, EditProviderBaseTests)
- **ServiceTests** - Tests for service implementations (e.g., EmailServiceTests)
- **DtoTests** - Tests for data transfer objects (e.g., PaginatedResultDtoTests)

### 1.1.3. Integration Tests

Integration tests that verify the interaction between components:

- **DataAccessTests** - Tests for data access and persistence
- **WebApiTests** - Tests for API controllers and endpoints
- **End-to-EndTests** - Tests that cover complete application flows

### 1.1.4. Mocks and Test Doubles

The project includes mock implementations for testing:

- **Mock Repository** - Uses Moq to create repository mocks
- **Mock DataContext** - Uses EF Core InMemory database provider
- **Mock Providers** - Simplified provider implementations for testing
- **Test Entities** - Sample entity implementations for tests

## 1.2. Test Patterns

The Tests project follows these testing patterns:

1. **Arrange-Act-Assert (AAA)** - Clear separation between test setup, execution, and verification
2. **Test Data Builders** - Fluent builders for creating test data
3. **Object Mother Pattern** - Factory methods for common test objects
4. **Test Categories** - Tests are organized by component type

## 1.3. Usage Example

```csharp
// Example of a repository unit test
[TestMethod]
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
    Assert.IsNotNull(result);
    Assert.AreEqual(1, result.Id);
    Assert.AreEqual("Test Product", result.Name);
}

// Example of a provider test with mocks
[TestMethod]
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
    Assert.IsNotNull(result);
    Assert.AreEqual("New Product", result.Name);
    mockRepository.Verify(r => r.CreateAsync(It.IsAny<Product>()), Times.Once);
    mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
}
```

## 1.4. Test Configuration

The Tests project includes configuration for various test frameworks and tools:

- **MSTest** - The primary test framework (v3.8.3)
- **Moq** - For creating mock objects (v4.20.70)
- **Microsoft.NET.Test.Sdk** - Testing infrastructure (v17.13.0)
- **EF Core InMemory** - For in-memory database testing

## 1.5. Running Tests

Tests can be executed using the following command:

```shell
dotnet test src/Paradigm.Enterprise.Tests
```

The project targets .NET 9.0, so ensure you have the appropriate SDK installed.

## 1.6. Continuous Integration

The Tests project is configured for continuous integration, providing:

1. **Automatic Test Execution** - Tests run on each build/pull request
2. **Code Coverage Reports** - Tracks code coverage of the tests
3. **Test Result Reporting** - Generates reports of test results
4. **Performance Metrics** - Monitors test execution performance
