using System;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer2;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class GetLastKnownPosition : Action
    {
        [SerializeField] private SharedVector3 lastKnownPosition;
        [SerializeField] private SharedBool searchDueToDamage;
        private AIEntity entity;
        private Entity enemy;
        private PositionTracker enemyTracker;
        private TargetKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            knowledgeBase = entity.TargetKb;
            navSystem = entity.NavigationSystem;

            enemyTracker = enemy.GetComponent<PositionTracker>();
        }

        public override TaskStatus OnUpdate()
        {
            float delay;
            Debug.Log("Entity " + gameObject.name + " will look because of damage? " + searchDueToDamage.Value);
            if (!searchDueToDamage.Value)
            {
                delay = Time.time - knowledgeBase.GetLastSightedTime();
                var (position, velocity) = enemyTracker.GetPositionAndVelocityFromDelay(delay);

                // Try to estimate the position of the enemy after it has gone out of sight
                var estimatedPosition = position + velocity * 0.1f;
                if (navSystem.IsPointOnNavMesh(estimatedPosition, out var point))
                {
                    lastKnownPosition.Value = estimatedPosition;
                    return TaskStatus.Success;
                }

                // Point wasn't valid, perhaps estimated position was OOB, use position
                lastKnownPosition.Value = estimatedPosition;
                return TaskStatus.Success;
            }

            delay = 0.2f;
            var (enemyPos, _) = enemyTracker.GetPositionAndVelocityFromDelay(delay);
            lastKnownPosition.Value = enemyPos;
            return TaskStatus.Success;
        }
    }
}