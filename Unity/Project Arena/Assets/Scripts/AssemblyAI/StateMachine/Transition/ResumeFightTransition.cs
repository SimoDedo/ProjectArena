using System;
using AssemblyAI.State;
using AssemblyLogging;

namespace AssemblyAI.StateMachine.Transition
{
    public class ResumeFightTransition : ITransition
    {
        private readonly Fight fight;
        private readonly Action action;
        
        public ResumeFightTransition(AIEntity entity, Action action)
        {
            this.action = action;
            fight = new Fight(entity);
        }

        public float GetScore()
        {
            return fight.FightTransitionScore(true);
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