using AssemblyAI.State;

namespace AssemblyAI.StateMachine
{
    public class EntityStateMachine : IStateMachine
    {
        private bool isIdle = true;
        private IState currentState;

        public EntityStateMachine(AIEntity entity)
        {
            currentState = new Wander(entity);
            currentState.Enter();
        }

        public void Update()
        {
            if (isIdle) return;
            var outgoing = currentState.OutgoingTransitions;
            var bestScore = float.MinValue;
            var nextState = currentState;
            foreach (var transition in outgoing)
            {
                var score = transition.GetScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    nextState = transition.GetNextState();
                }
            }

            if (currentState != nextState)
            {
                currentState.Exit();
                nextState.Enter();
                currentState = nextState;
            }
            
            currentState.Update();
        }

        public void SetIsIdle(bool idle = true)
        {
            isIdle = idle;
        }
    }
}