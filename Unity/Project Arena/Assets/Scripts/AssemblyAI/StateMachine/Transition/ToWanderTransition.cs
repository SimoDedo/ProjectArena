using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ToWanderTransition : ITransition
    {
        private Wander wander;

        public ToWanderTransition(AIEntity entity)
        {
            wander = new Wander(entity);
        }

        public ToWanderTransition(Wander wander)
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
    }
}