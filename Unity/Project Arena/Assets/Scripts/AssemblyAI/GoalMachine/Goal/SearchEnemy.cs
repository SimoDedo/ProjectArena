using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Sensors;
using BehaviorDesigner.Runtime;
using UnityEngine;

namespace AssemblyAI.GoalMachine.Goal
{
    public class SearchEnemy : IGoal
    {
        private const float NO_TIME = -1;
        private readonly AIEntity entity;
        private readonly TargetKnowledgeBase targetKb;
        private readonly DamageSensor damageSensor;
        private readonly ExternalBehaviorTree externalBt;
        private readonly BehaviorTree behaviorTree;
        private float startSearchTime = NO_TIME;

        public SearchEnemy(AIEntity entity)
        {
            this.entity = entity;
            targetKb = entity.TargetKb;
            damageSensor = entity.DamageSensor;
            externalBt = Resources.Load<ExternalBehaviorTree>("Behaviors/SearchEnemy");
            behaviorTree = entity.gameObject.AddComponent<BehaviorTree>();
            behaviorTree.StartWhenEnabled = false;
            behaviorTree.RestartWhenComplete = true;
            behaviorTree.ExternalBehavior = externalBt;
            behaviorTree.EnableBehavior();
            BehaviorManager.instance.UpdateInterval = UpdateIntervalType.Manual;

        }

        public float GetScore()
        {
            var searchDueToLoss = targetKb.HasLostTarget();
            var searchDueToDamage = !targetKb.HasSeenTarget() && damageSensor.WasDamagedRecently;
            if (entity.GetEnemy().isAlive && (searchDueToLoss || searchDueToDamage))
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (startSearchTime == NO_TIME)
                    return 0.7f;
                // Slowly decrease want to search. After 5 secs, it's zero
                return 1f - (Time.time - startSearchTime) / 5f;
            }

            return 0f;
        }
        
        public void Enter()
        {
            behaviorTree.EnableBehavior();
            startSearchTime = Time.time;
        }

        public void Update()
        {
            BehaviorManager.instance.Tick(behaviorTree);
        }

        public void Exit()
        {
            startSearchTime = NO_TIME;
            behaviorTree.DisableBehavior();
        }
    }
}