using System.Reflection;

namespace Paradigm.Enterprise.Domain.StateMachines
{
    /// <summary>
    /// Creates convention-named state implementations from the assembly containing their state contract.
    /// </summary>
    public static class StateFactory
    {
        /// <summary>
        /// Creates the type named <c>{StateNamespace}.{stateName}State</c> using a constructor that accepts the context.
        /// </summary>
        /// <typeparam name="TState">The common state contract.</typeparam>
        /// <param name="stateName">The state-name portion of the concrete type name.</param>
        /// <param name="context">The context passed to the concrete state's constructor.</param>
        /// <returns>The created state, or <see langword="null"/> when the created object is not a <typeparamref name="TState"/>.</returns>
        /// <exception cref="ArgumentException">No type matching the naming convention exists.</exception>
        /// <exception cref="MissingMethodException">The state type has no compatible constructor.</exception>
        public static TState? Create<TState>(string stateName, IStateContext<TState> context) where TState : class, IState<TState>
        {
            var stateType = typeof(TState);
            var typeName = $"{stateType.Namespace}.{stateName}State";
            var type = stateType.GetTypeInfo().Assembly.GetType(typeName);

            if (type is null)
                throw new ArgumentException($"The state '{typeName}' type can not be found.");

            return Activator.CreateInstance(type, context) as TState;
        }
    }
}
