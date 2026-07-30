using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Mappers;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Providers;
using Paradigm.Enterprise.Services.Core;
using System.Reflection;

namespace Paradigm.Enterprise.WebApi.Extensions
{
    /// <summary>
    /// Discovers and registers Paradigm providers, repositories, services, mappers, entities, and DTOs.
    /// </summary>
    /// <remarks>
    /// Discovery includes the supplied assemblies and their directly referenced assemblies. Assemblies
    /// that cannot be loaded are skipped. Provider, repository, and mapper registration is transient;
    /// service registration is singleton. Concrete and convention-matched service interfaces are
    /// registered separately, so resolving both keys can create two distinct singleton instances.
    /// </remarks>
    /// <example>
    /// Register application conventions from an explicit assembly:
    /// <code>
    /// var applicationAssembly = typeof(OrdersProvider).Assembly;
    ///
    /// services
    ///     .RegisterProviders(applicationAssembly)
    ///     .RegisterRepositories(applicationAssembly)
    ///     .RegisterMappers(applicationAssembly)
    ///     .RegisterEntities(applicationAssembly)
    ///     .RegisterDtos(applicationAssembly);
    ///
    /// services.RegisterServices(
    ///     [typeof(BlobStorageService)],
    ///     applicationAssembly);
    /// services.RegisterBlobStorageAccountUsingConnectionString("BlobStorage");
    /// </code>
    /// A provider or service interface is registered by convention only when it is named after its
    /// implementation, for example <c>IOrdersProvider</c>/<c>OrdersProvider</c>.
    /// </example>
    public static class ServiceCollectionExtensions
    {
        #region Public Methods

        /// <summary>
        /// Registers the providers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// A provider implements more than one interface whose simple name is
        /// <c>I{ImplementationName}</c>.
        /// </exception>
        /// <remarks>
        /// A provider with no convention-matched interface is skipped. A provider with more than one
        /// match, including same-named interfaces from different namespaces, causes registration to fail.
        /// </remarks>
        public static IServiceCollection RegisterProviders(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(IProvider).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
            {
                var concreteInterface = type.GetInterfaces().SingleOrDefault(x => x.Name == $"I{type.Name}");

                if (concreteInterface is null)
                    continue;

                services.AddTransient(concreteInterface, type);

                var genericInterfaces = concreteInterface.GetInterfaces().Where(x => x.IsGenericType).ToList();

                foreach (var genericInterface in genericInterfaces.Where(genericInterface => genericInterface is not null))
                    services.AddTransient(genericInterface, type);
            }

            return services;
        }

        /// <summary>
        /// Registers the repositories.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// A discovered repository does not implement exactly one convention interface named
        /// <c>I{ImplementationName}</c>.
        /// </exception>
        /// <remarks>
        /// Each public concrete <see cref="IRepository"/> must implement exactly one interface named
        /// after it, such as <c>IOrderRepository</c> for <c>OrderRepository</c>. In addition to that
        /// convention interface, the scanner registers only the first inherited generic interface
        /// returned by reflection. Consumers should rely on the convention interface rather than assume
        /// every generic repository contract is registered.
        /// </remarks>
        public static IServiceCollection RegisterRepositories(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(IRepository).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
            {
                var concreteInterface = type.GetInterfaces().Single(x => x.Name == $"I{type.Name}");
                var genericInterface = concreteInterface.GetInterfaces().FirstOrDefault(x => x.IsGenericType);

                if (genericInterface is not null) services.AddTransient(genericInterface, type);
                services.AddTransient(concreteInterface, type);
            }

            return services;
        }

        /// <summary>
        /// Registers the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="ignore">The ignore.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <remarks>
        /// Each concrete service and convention-matched interface is registered independently with
        /// the implementation type. They are not aliases guaranteed to resolve the same singleton
        /// instance; stateful consumers should consistently use one service key. The scanner does not
        /// validate constructors, so implementations must be activatable by dependency injection.
        /// Ignore <c>BlobStorageService</c> when its assembly is scanned and use one of the dedicated
        /// blob-storage registration methods; its constructor is private, and a scanned singleton
        /// registration can also supersede the intended scoped interface registration.
        /// </remarks>
        public static IServiceCollection RegisterServices(this IServiceCollection services, Type[] ignore, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(IService).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
            {
                if (ignore.Contains(type))
                    continue;

                services.AddSingleton(type);
                var interfaces = type.GetInterfaces().Where(x => x.Name == $"I{type.Name}").ToList();

                foreach (var singleInterface in interfaces)
                    services.AddSingleton(singleInterface, type);
            }

            return services;
        }

        /// <summary>
        /// Registers the mappers.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        /// <exception cref="InvalidOperationException">
        /// A discovered mapper does not implement exactly one generic interface.
        /// </exception>
        /// <remarks>
        /// Each public concrete <see cref="IMapper"/> must expose exactly one generic mapper interface;
        /// the scanner uses that interface as its service key and also registers the concrete type.
        /// </remarks>
        public static IServiceCollection RegisterMappers(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(IMapper).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
            {
                var genericInterface = type.GetInterfaces().Single(x => x.IsGenericType);

                services.AddTransient(genericInterface, type);
                services.AddTransient(type);
            }

            return services;
        }

        /// <summary>
        /// Registers the entities.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection RegisterEntities(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(EntityBase).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
                services.AddTransient(type);

            return services;
        }

        /// <summary>
        /// Registers the dtos.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection RegisterDtos(this IServiceCollection services, params Assembly[] assemblies)
        {
            var types = GetTypes(x => typeof(DtoBase).IsAssignableFrom(x) && x is { IsAbstract: false, IsPublic: true }, assemblies);

            foreach (var type in types)
                services.AddTransient(type);

            return services;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the types that inherit.
        /// </summary>
        /// <param name="filter">Function to filter the types.</param>
        /// <param name="assemblies">Optional assemblies to use as entry point. If no assembly is provided, the system will use the entry assembly. By default the system will use the entry assembly.</param>
        /// <returns>The distinct matching types discovered in loadable assemblies.</returns>
        private static IEnumerable<TypeInfo> GetTypes(Func<TypeInfo, bool> filter, params Assembly?[] assemblies)
        {
            if (assemblies is null || assemblies.Length == 0)
                assemblies = [Assembly.GetEntryAssembly()];

            var assemblyNames = assemblies
                .SelectMany(x => x?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>())
                .Union(assemblies.Select(x => x?.GetName()))
                .ToList();

            var assemblyLookups = new List<Assembly>();

            foreach (var assemblyName in assemblyNames)
                try
                {
                    if (assemblyName is not null)
                        assemblyLookups.Add(Assembly.Load(assemblyName));
                }
                catch
                {
                    // ignore assembles that can't be loaded.
                }

            return assemblyLookups.SelectMany(x => x.DefinedTypes).Where(filter).Distinct().ToList();
        }

        #endregion
    }
}
