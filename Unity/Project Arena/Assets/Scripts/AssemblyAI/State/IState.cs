namespace AssemblyAI.State
{
    public interface IState
    {
        float CalculateTransitionScore();
        public void Enter();
        public void Update();
        public void Exit();
    }
}

