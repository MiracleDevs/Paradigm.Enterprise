namespace Paradigm.Enterprise.Domain.StateMachines
{
    public interface IStateContext<out TState> where TState : IState<TState>
    {
        /// <summary>
        /// Gets the state of the current.
        /// </summary>
        /// <returns></returns>
        TState? GetCurrentState();

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// Initializes the state.
        /// </summary>
        void InitializeState();

        /// <summary>
        /// Sets the state.
        /// </summary>
        /// <param name="state">The state.</param>
        void SetState(int state);
    }
}