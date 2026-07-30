namespace Paradigm.Enterprise.Domain.StateMachines
{
    /// <summary>
    /// Represents a named state associated with a state-machine context.
    /// </summary>
    /// <typeparam name="TState">The common state contract implemented by concrete states.</typeparam>
    public interface IState<out TState>
        where TState : IState<TState>
    {
        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context whose current state this instance can inspect or transition.
        /// </value>
        IStateContext<TState> Context { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The implementation-defined state name.
        /// </value>
        string Name { get; }
    }
}
