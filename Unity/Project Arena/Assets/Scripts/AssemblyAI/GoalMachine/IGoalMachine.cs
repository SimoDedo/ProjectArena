namespace AssemblyAI.GoalMachine
{
    public interface IGoalMachine
    {
        void Update();
        void SetIsIdle(bool idle = true);
    }
}