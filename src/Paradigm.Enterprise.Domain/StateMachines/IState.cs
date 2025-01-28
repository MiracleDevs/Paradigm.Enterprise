namespace Paradigm.Enterprise.Domain.StateMachines
{
    public interface IState<out TState>
        where TState : IState<TState>
    {
        /// <summary>
        /// Gets the context.
        /// </summary>
        /// <value>
        /// The context.
        /// </value>
        IStateContext<TState> Context { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }
    }
}