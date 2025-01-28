using System.Reflection;

namespace Paradigm.Enterprise.Domain.StateMachines
{
    public static class StateFactory
    {
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