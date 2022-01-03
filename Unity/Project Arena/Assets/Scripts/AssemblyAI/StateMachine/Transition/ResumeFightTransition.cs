using AssemblyAI.StateMachine.State;

namespace AssemblyAI.StateMachine.Transition
{
    public class ResumeFightTransition : ITransition
    {
        private readonly Fight fight;

        public ResumeFightTransition(AIEntity entity)
        {
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
    }
}