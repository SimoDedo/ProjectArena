namespace AssemblyAI.GoalMachine.Goal
{
    public interface IGoal
    {
        float GetScore();
        void Enter();
        void Update();
        void Exit();
    }
}