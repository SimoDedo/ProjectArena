namespace AssemblyAI.GoalMachine.Goal
{
    public class NoGoal : IGoal
    {
        public float GetScore()
        {
            return float.MinValue;
        }

        public void Enter() { }

        public void Update() { }

        public void Exit() { }
    }
}