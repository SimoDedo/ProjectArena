using System;
using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class PickupSelfLoop : ITransition
    {
        private readonly LookForPickups lookForPickups;

        public PickupSelfLoop(LookForPickups pickups)
        {
            lookForPickups = pickups;
        }
        public float GetScore()
        {
            return lookForPickups.CalculateTransitionScore();
        }

        public IState GetNextState()
        {
            return lookForPickups;
        }

        public void OnActivate()
        { }
    }
}