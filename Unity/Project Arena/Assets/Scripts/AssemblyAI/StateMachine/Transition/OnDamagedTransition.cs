using AssemblyAI.StateMachine.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class OnDamagedTransition : ITransition
    {
        private SearchEnemy search;
        public OnDamagedTransition(AIEntity entity)
        {
            search = new SearchEnemy(entity, true);
        }

        public float GetScore()
        {
            return search.DamagedTransitionScore();
        }

        public IState GetNextState()
        {
            return search;
        }
    }
}