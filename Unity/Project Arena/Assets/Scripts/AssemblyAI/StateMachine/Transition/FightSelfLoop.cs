using AssemblyAI.State;

namespace AssemblyAI.StateMachine.Transition
{
    // Used for
    // Fight -> Fight
    public class FightSelfLoop : ITransition
    {
        private readonly Fight fight;
        
        public FightSelfLoop(Fight fight)
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
        
        public void OnActivate() { }
    }
}