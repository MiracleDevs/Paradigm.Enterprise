using System.Runtime.CompilerServices;

namespace Paradigm.Enterprise.Domain.StateMachines
{
    /// <summary>
    /// Reports that a named transition is invalid for the context's current state.
    /// </summary>
    /// <typeparam name="TState">The common state contract.</typeparam>
    public class StateTransitionException<TState> : Exception where TState : IState<TState>
    {
        /// <summary>
        /// Initializes an exception describing the rejected transition.
        /// </summary>
        /// <param name="context">The state-machine context.</param>
        /// <param name="transitionName">
        /// The transition name. When omitted, the compiler supplies the caller member name.
        /// </param>
        public StateTransitionException(IStateContext<TState> context, [CallerMemberName] string? transitionName = default) : base($"Cannot {transitionName} '{context.GetName()}' when is {context.GetCurrentState()?.Name}.")
        {
        }
    }
}
