namespace AI.GoalMachine
{
    /// <summary>
    /// Represents a system capable of dealing with goals.
    /// </summary>
    public interface IGoalMachine
    {
        /// <summary>
        /// Recalculates the current goal and executes it.
        /// </summary>
        void Update();
        /// <summary>
        /// Sets whether the goal machine Update method should work or not.
        /// </summary>
        void SetIsIdle(bool idle = true);
        /// <summary>
        /// Resets the state of the goal machine to the default one.
        /// </summary>
        void Reset();
    }
}