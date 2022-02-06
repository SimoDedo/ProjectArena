using AssemblyAI.GoalMachine.Goal;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.GoalMachine
{
    public class EntityGoalMachine : IGoalMachine
    {
        private bool isIdle = true;
        private int currentGoalIndex;
        private readonly IGoal[] goals;
        private IGoal CurrentGoal => goals[currentGoalIndex];

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