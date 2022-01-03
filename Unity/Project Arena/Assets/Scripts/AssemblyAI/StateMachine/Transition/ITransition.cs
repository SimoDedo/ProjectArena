using AssemblyAI.StateMachine.State;

namespace AssemblyAI.StateMachine
{
    public interface ITransition
    {
        float GetScore();
        IState GetNextState();
    }
}