using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Select wander destination.
    /// Deals with selecting a wander destination and reaching it.
    /// The goal plan is contained in a behaviour tree.
    /// </summary>
    public class Wander : IGoal
    {
        private readonly BehaviorTree behaviorTree;
        private readonly ExternalBehaviorTree externalBt;

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
            return 0.05f;
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