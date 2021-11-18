namespace AssemblyAI.StateMachine
{
    public interface ITransition
    {
        float GetScore();
        IState GetNextState();
    }
}