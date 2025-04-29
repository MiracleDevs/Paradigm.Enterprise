using Microsoft.Extensions.DependencyInjection;

namespace Paradigm.Enterprise.Tests.Providers;

[TestClass]
[ExcludeFromCodeCoverage]
public class ProviderBaseTests
{
    // Test provider implementation
    public class TestProvider : ProviderBase
    {
        public TestProvider(IServiceProvider serviceProvider) : base(serviceProvider) { }
        
        public T GetProviderTest<T>() where T : IProvider
        {
            return GetProvider<T>();
        }
    }
    
    // Test provider interface
    public interface ITestDependencyProvider : IProvider { }
    
    // Test provider implementation
    public class TestDependencyProvider : ITestDependencyProvider
    {
        public TestDependencyProvider() { }
    }

    private ServiceProvider _serviceProvider;
    private TestProvider _provider;
    private ITestDependencyProvider _dependencyProvider;

    [TestInitialize]
    public void Initialize()
    {
        _dependencyProvider = new TestDependencyProvider();
        
        var services = new ServiceCollection();
        services.AddSingleton<ITestDependencyProvider>(_dependencyProvider);
        _serviceProvider = services.BuildServiceProvider();
        
        _provider = new TestProvider(_serviceProvider);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _serviceProvider.Dispose();
    }

    [TestMethod]
    public void GetProvider_ShouldReturnRegisteredProvider()
    {
        // Act
        var result = _provider.GetProviderTest<ITestDependencyProvider>();
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(_dependencyProvider, result);
    }
} 