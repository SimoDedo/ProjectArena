using AI.KnowledgeBase;
using AssemblyEntity.Component;
using AssemblyLogging;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.GoalMachine.Goal
{
    public class Fight : IGoal
    {
        private const float LOW_HEALTH_PENALTY = 0.6f;
        private readonly AIEntity entity;
        private readonly TargetKnowledgeBase targetKb;
        private readonly GunManager gunManager;
        private readonly ExternalBehaviorTree externalBt;
        private readonly BehaviorTree behaviorTree;

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
            var canSee = targetKb.HasSeenTarget() && entity.GetEnemy().isAlive;
            var inverseHealthPercentage = 1f - (float) entity.Health / entity.MaxHealth;
            return canSee ? 0.8f - inverseHealthPercentage * LOW_HEALTH_PENALTY : 0.0f;
        }


        public void Enter()
        {
            FightingStatusGameEvent.Instance.Raise(
                new FightingStatus {entityId = entity.GetID(), isActivelyFighting = true}
            );
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
            // Resources.UnloadAsset(externalBt);
            // Object.Destroy(behaviorTree);
        }
    }
}