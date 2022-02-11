using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.GoalMachine.Goal
{
    public class Wander : IGoal
    {
        private readonly ExternalBehaviorTree externalBt;
        private readonly BehaviorTree behaviorTree;
        
        public Wander(AIEntity entity)
        {
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/Wander");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            return 0.2f;
        }

        public void Enter()
        {
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.RestartBehavior(behaviorTree);
        }

        public void Update()
        {
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            behaviorTree.DisableBehavior();
        }
    }
}