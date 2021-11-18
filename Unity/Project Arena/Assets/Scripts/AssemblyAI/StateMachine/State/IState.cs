namespace AssemblyAI.StateMachine
{
    public interface IState
    {
        ITransition[] OutgoingTransitions { get; }
        void Enter();
        void Update();
        void Exit();
    }
}