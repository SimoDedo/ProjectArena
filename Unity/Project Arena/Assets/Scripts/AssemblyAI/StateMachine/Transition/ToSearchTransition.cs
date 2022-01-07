using AssemblyAI.StateMachine.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ToSearchTransition : ITransition
    {
        private readonly SearchEnemy search;
        private readonly bool searchDueToDamage;
        public ToSearchTransition(AIEntity entity)
        {
            search = new SearchEnemy(entity);
        }

        public ToSearchTransition(SearchEnemy search, bool searchDueToDamage)
        {
            this.search = search;
            this.searchDueToDamage = searchDueToDamage;
        }

        public float GetScore()
        {
            return searchDueToDamage ? search.DamagedTransitionScore() : search.LostEnemyTransitionScore();
        }

        public IState GetNextState()
        {
            return search;
        }
    }
}