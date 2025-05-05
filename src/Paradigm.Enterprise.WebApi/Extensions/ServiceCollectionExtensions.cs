using Paradigm.Enterprise.Domain.Dtos;
using Paradigm.Enterprise.Domain.Entities;
using Paradigm.Enterprise.Domain.Mappers;
using Paradigm.Enterprise.Domain.Repositories;
using Paradigm.Enterprise.Providers;
using Paradigm.Enterprise.Services.Core;
using System.Reflection;

namespace Paradigm.Enterprise.WebApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        #region Public Methods

        /// <summary>
        /// Registers the providers.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="assemblies">The assemblies.</param>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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