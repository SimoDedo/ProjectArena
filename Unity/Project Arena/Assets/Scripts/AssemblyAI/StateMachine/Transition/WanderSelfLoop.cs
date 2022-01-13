using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class WanderSelfLoop: ITransition
    {
        private readonly Wander wander;

        public WanderSelfLoop(Wander wander)
        {
            this.wander = wander;
        }

        public float GetScore()
        {
            return wander.CalculateTransitionScore();
        }

        public IState GetNextState()
        {
            return wander;
        }

        public void OnActivate() { }
    }
}