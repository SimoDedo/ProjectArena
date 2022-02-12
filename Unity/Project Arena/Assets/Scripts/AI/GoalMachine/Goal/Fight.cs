using AI.AI.Layer2;
using BehaviorDesigner.Runtime;
using Entity.Component;
using UnityEngine;

namespace AI.GoalMachine.Goal
{
    /// <summary>
    /// Fighting goal. Deals with moving, shooting, finding cover, reloading and so on.
    /// The goal plan is contained in a behaviour tree.
    /// </summary>
    public class Fight : IGoal
    {
        private const float LOW_HEALTH_PENALTY = 0.6f;
        private readonly BehaviorTree behaviorTree;
        private readonly AIEntity entity;
        private readonly ExternalBehaviorTree externalBt;
        private readonly GunManager gunManager;
        private readonly TargetKnowledgeBase targetKb;

        public Fight(AIEntity entity)
        {
            this.entity = entity;
            targetKb = entity.TargetKb;
            gunManager = entity.GunManager;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/NewFight");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
        }

        public float GetScore()
        {
            if (!gunManager.HasAmmo()) return 0;
            // TODO maybe we see enemy, but we want to run away?
            var canSee = targetKb.HasSeenTarget() && entity.GetEnemy().IsAlive;
            var inverseHealthPercentage = 1f - (float) entity.Health / entity.MaxHealth;
            return canSee ? 0.8f - inverseHealthPercentage * LOW_HEALTH_PENALTY : 0.0f;
        }


        public void Enter()
        {
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.RestartBehavior(behaviorTree);
        }

        public void Update()
        {
            entity.IsFocusingOnEnemy = true;
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            entity.IsFocusingOnEnemy = false;
            behaviorTree.DisableBehavior();
            // Resources.UnloadAsset(externalBt);
            // Object.Destroy(behaviorTree);
        }
    }
}