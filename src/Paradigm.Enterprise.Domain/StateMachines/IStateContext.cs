namespace Paradigm.Enterprise.Domain.StateMachines
{
    /// <summary>
    /// Defines the host operations used by a state-machine state.
    /// </summary>
    /// <typeparam name="TState">The common state contract.</typeparam>
    public interface IStateContext<out TState> where TState : IState<TState>
    {
        /// <summary>
        /// Gets the current state.
        /// </summary>
        /// <returns>The active state, or <see langword="null"/> before initialization.</returns>
        TState? GetCurrentState();

        /// <summary>
        /// Gets the display name of the object governed by the state machine.
        /// </summary>
        /// <returns>The context name used in transition diagnostics.</returns>
        string GetName();

        /// <summary>
        /// Initializes the context's starting state.
        /// </summary>
        void InitializeState();

        /// <summary>
        /// Selects a state using the context's implementation-defined numeric value.
        /// </summary>
        /// <param name="state">The numeric state value to select.</param>
        void SetState(int state);
    }
}
