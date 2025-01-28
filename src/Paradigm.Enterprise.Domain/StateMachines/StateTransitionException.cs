using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Domain.StateMachines
{
    public class StateTransitionException<TState> : Exception where TState : IState<TState>
    {
        public StateTransitionException(IStateContext<TState> context, [CallerMemberName] string? transitionName = default) : base($"Cannot {transitionName} '{context.GetName()}' when is {context.GetCurrentState()?.Name}.")
        {
        }
    }
}