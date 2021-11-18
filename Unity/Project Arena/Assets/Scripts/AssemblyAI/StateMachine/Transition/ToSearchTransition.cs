using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ToSearchTransition : ITransition
    {
        private readonly SearchEnemy search;
        public ToSearchTransition(AIEntity entity)
        {
            search = new SearchEnemy(entity);
        }

        public ToSearchTransition(SearchEnemy search)
        {
            this.search = search;
        }

        public float GetScore()
        {
            return search.LostEnemyTransitionScore();
        }

        public IState GetNextState()
        {
            return search;
        }
    }
}