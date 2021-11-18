using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class OnEnemyInSightTransition : ITransition
    {
        private readonly Fight fight;

        public OnEnemyInSightTransition(AIEntity entity)
        {
            fight = new Fight(entity);
        }
        
        public OnEnemyInSightTransition(Fight fight)
        {
            this.fight = fight;
        }

        public float GetScore()
        {
            return fight.FightTransitionScore();
        }

        public IState GetNextState()
        {
            return fight;
        }
    }
}