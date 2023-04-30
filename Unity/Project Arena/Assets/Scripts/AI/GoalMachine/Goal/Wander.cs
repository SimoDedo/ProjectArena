using BehaviorDesigner.Runtime;
using Bonsai.Core;
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
        private readonly AIEntity entity;
        private BonsaiTreeComponent bonsaiBehaviorTree;
        private readonly BehaviourTree blueprint;

        public Wander(AIEntity entity)
        {
            this.entity = entity;
            blueprint = Resources.Load<BehaviourTree>("Behaviors/BonsaiWander");
            // behaviorTree.StartWhenEnabled = false;
            // behaviorTree.RestartWhenComplete = true;
            // behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            return 0.05f;
        }

        public void Enter()
        {
            if (bonsaiBehaviorTree != null)
            {
                Object.Destroy(bonsaiBehaviorTree);
            }
            
            bonsaiBehaviorTree = entity.gameObject.AddComponent<BonsaiTreeComponent>();
            bonsaiBehaviorTree.SetBlueprint(blueprint);
        }

        public void Update()
        {
            bonsaiBehaviorTree.Tick();
        }

        public void Exit()
        {
        }
    }
}