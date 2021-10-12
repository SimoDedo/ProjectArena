namespace AssemblyAI.State
{
    public class Idle : IState
    {
        public float CalculateTransitionScore()
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