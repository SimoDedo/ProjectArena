namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Interface of a generic goal for a goal machine.
    /// </summary>
    public interface IGoal
    {
        /// <summary>
        /// Returns the score of the goal.
        /// </summary>
        float GetScore();

        /// <summary>
        /// To be called when a goal is about to be activated and used.
        /// </summary>
        void Enter();

        /// <summary>
        /// Executes any step necessary to run the goal.
        /// </summary>
        void Update();

        /// <summary>
        /// To be called when a goal is about to be deactivated.
        /// </summary>
        void Exit();
    }
}