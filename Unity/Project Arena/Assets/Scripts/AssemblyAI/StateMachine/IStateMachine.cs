namespace AssemblyAI.StateMachine
{
    public interface IStateMachine
    {
        void Update();
        void SetIsIdle(bool idle = true);
    }
}