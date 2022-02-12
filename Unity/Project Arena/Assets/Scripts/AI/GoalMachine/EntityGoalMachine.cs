using AI.GoalMachine.Goal;
using BehaviorDesigner.Runtime;

namespace AI.GoalMachine
{
    /// <summary>
    /// Default goal machine of an entity.
    /// </summary>
    public class EntityGoalMachine : IGoalMachine
    {
        private readonly IGoal[] goals;
        private int currentGoalIndex;
        private bool isIdle = true;

        public EntityGoalMachine(AIEntity entity)
        {
            Behavior.CreateBehaviorManager();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;
            goals = new IGoal[]
            {
                new NoGoal(), new Wander(entity), new Fight(entity), new SearchEnemy(entity),
                new LookForPickups(entity)
            };
            currentGoalIndex = 0;
            CurrentGoal.Enter();
        }

        private IGoal CurrentGoal => goals[currentGoalIndex];

        public void Update()
        {
            if (isIdle) return;
            var bestScore = float.MinValue;
            var bestIndex = currentGoalIndex;
            for (var i = 0; i < goals.Length; i++)
            {
                var score = goals[i].GetScore();
                if (score > bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            if (currentGoalIndex != bestIndex)
            {
                CurrentGoal.Exit();
                currentGoalIndex = bestIndex;
                CurrentGoal.Enter();
            }

            CurrentGoal.Update();
        }

        public void SetIsIdle(bool idle = true)
        {
            isIdle = idle;
        }

        public void Reset()
        {
            CurrentGoal.Exit();
            currentGoalIndex = 0;
            CurrentGoal.Enter();
        }
    }
}