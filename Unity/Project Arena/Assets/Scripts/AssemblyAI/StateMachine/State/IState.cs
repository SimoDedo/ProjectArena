namespace AssemblyAI.StateMachine.State
{
    public interface IState
    {
        ITransition[] OutgoingTransitions { get; }
        void Enter();
        void Update();
        void Exit();
    }
}