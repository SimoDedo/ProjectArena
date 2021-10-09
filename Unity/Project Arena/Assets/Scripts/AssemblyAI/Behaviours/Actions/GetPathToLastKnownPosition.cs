using System;
using AI.KnowledgeBase;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class GetPathToLastKnownPosition : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathChosen;
        private AIEntity entity;
        private Entity enemy;
        private PositionTracker enemyTracker;
        private TargetKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            enemyTracker = enemy.GetComponent<PositionTracker>();
            knowledgeBase = enemy.GetComponent<TargetKnowledgeBase>();
            navSystem = enemy.GetComponent<NavigationSystem>();
        }

        public override TaskStatus OnUpdate()
        {
            var delay = Time.time - knowledgeBase.GetLastKnownPositionTime();
            var (position, velocity) = enemyTracker.GetPositionAndVelocityFromDelay(delay);

            // Try to estimate the position of the enemy after it has gone out of sight
            var estimatedPosition = position + velocity * 0.1f;
            var path = navSystem.CalculatePath(estimatedPosition);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                pathChosen.Value = path;
                return TaskStatus.Success;
            }
            // Path wasn't valid, perhaps estimated position was OOB, use position
            path = navSystem.CalculatePath(position);
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                pathChosen.Value = path;
                return TaskStatus.Success;
            }
            // This shouldn't happen, abort search! 
            // TODO Cannot abort from tree, throw exception for now
            throw new Exception("Couldn't search for lost enemy");
        }
    }
}