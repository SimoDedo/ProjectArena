using AssemblyAI.StateMachine.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ToPickupTransition : ITransition
    {
        private readonly LookForPickups lookForPickups;

        public ToPickupTransition(AIEntity entity)
        {
            lookForPickups = new LookForPickups(entity);
        }
        public ToPickupTransition(LookForPickups lookForPickups)
        {
            this.lookForPickups = lookForPickups;
        }

        public float GetScore()
        {
            return lookForPickups.CalculateTransitionScore();
        }

        public IState GetNextState()
        {
            return lookForPickups;
        }
    }
}