using System;
using Accord.Statistics;
using AI.KnowledgeBase;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class GetLastKnownPosition : Action
    {
        [SerializeField] private SharedVector3 lastKnownPosition;
        private AIEntity entity;
        private Entity enemy;
        private PositionTracker enemyTracker;
        private TargetKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            knowledgeBase = GetComponent<TargetKnowledgeBase>();
            navSystem = GetComponent<NavigationSystem>();
            
            enemyTracker = enemy.GetComponent<PositionTracker>();
        }

        public override TaskStatus OnUpdate()
        {
            var delay = Time.time - knowledgeBase.GetLastKnownPositionTime();
            var (position, velocity) = enemyTracker.GetPositionAndVelocityFromDelay(delay);

            // Try to estimate the position of the enemy after it has gone out of sight
            var estimatedPosition = position + velocity * 0.1f;
            if (navSystem.IsPointOnNavMesh(estimatedPosition, out var point))
            {
                point.y += 4f;
                lastKnownPosition.Value = point;
                return TaskStatus.Success;
            }
            // Point wasn't valid, perhaps estimated position was OOB, use position
            lastKnownPosition.Value = estimatedPosition;
            return TaskStatus.Success;
        }
    }
}