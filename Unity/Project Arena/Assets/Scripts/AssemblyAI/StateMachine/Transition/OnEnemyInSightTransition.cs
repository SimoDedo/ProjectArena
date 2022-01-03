using System;
using AssemblyAI.State;
using AssemblyLogging;

namespace AssemblyAI.StateMachine.Transition
{
    // Used for
    // Wander -> Fight
    // Pickup -> Fight
    public class OnEnemyInSightTransition : ITransition
    {
        private readonly Fight fight;
        private Action action;
        
        public OnEnemyInSightTransition(AIEntity entity, Action action = null)
        {
            this.action = action;
            fight = new Fight(entity);
        }

        public float GetScore()
        {
            return fight.FightTransitionScore();
        }

        public IState GetNextState()
        {
            return fight;
        }

        public void OnActivate()
        {
            action?.Invoke();
        }
    }
}