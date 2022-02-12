namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Empty goal. Can be used as a default one.
    /// </summary>
    public class NoGoal : IGoal
    {
        public float GetScore()
        {
            return float.MinValue;
        }

        public void Enter()
        {
        }

        public void Update()
        {
        }

        public void Exit()
        {
        }
    }
}