using System;
using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ToSearchTransition : ITransition
    {
        private readonly SearchEnemy search;
        private readonly Action action;

        public ToSearchTransition(AIEntity entity, Action action = null)
        {
            this.action = action;
            search = new SearchEnemy(entity);
        }

        public float GetScore()
        {
            return search.LostEnemyTransitionScore();
        }

        public IState GetNextState()
        {
            return search;
        }

        public void OnActivate()
        {
            action?.Invoke();
        }
    }
}