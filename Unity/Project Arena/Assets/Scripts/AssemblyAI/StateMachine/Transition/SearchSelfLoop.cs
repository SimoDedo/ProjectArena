using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class SearchSelfLoop : ITransition
    {
        private readonly SearchEnemy search;

        public SearchSelfLoop(SearchEnemy search)
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

        public void OnActivate() { }
    }
}