using System;
using AI.AI.Layer1;
using AI.AI.Layer2;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entity;
using Others;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class GetLastKnownPosition : Action
    {
        [SerializeField] private SharedSelectedPathInfo lastKnownPositionPath;
        private AIEntity entity;
        private Entity.Entity enemy;
        private PositionTracker enemyTracker;
        private TargetKnowledgeBase knowledgeBase;
        private NavigationSystem navSystem;
        private DamageSensor damageSensor;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            enemy = entity.GetEnemy();
            knowledgeBase = entity.TargetKb;
            navSystem = entity.NavigationSystem;
            damageSensor = entity.DamageSensor;
            enemyTracker = enemy.GetComponent<PositionTracker>();
        }

        public override TaskStatus OnUpdate()
        {
            var damageTime = damageSensor.WasDamagedRecently
                ? (damageSensor.LastTimeDamaged)
                : float.MinValue;

            var lossTime = knowledgeBase.LastTimeDetected;

            if (damageTime > lossTime)
            {
                // The most recent event was getting damaged, so use this knowledge to guess its position
                return EstimateEnemyPositionFromDamage();
            }
            // The most recent event was losing the enemy, so use this knowledge to guess its position
            return EstimateEnemyPositionFromKnowledge();
        }

        private TaskStatus EstimateEnemyPositionFromKnowledge()
        {
            var delay = Time.time - knowledgeBase.LastTimeDetected;
            var (delayedPosition, velocity) = enemyTracker.GetPositionAndVelocityFromDelay(delay);
            
            // Try to estimate the position of the enemy after it has gone out of sight
            var estimatedPosition = delayedPosition + velocity * 0.1f;

            var pathToEstimatedPos = navSystem.CalculatePath(estimatedPosition);
            if (pathToEstimatedPos.IsComplete())
            {
                lastKnownPositionPath.Value = pathToEstimatedPos;
                return TaskStatus.Success;
                
            }

            // Point wasn't valid, perhaps estimated position was OOB, use position
            var pathToDelayedPosition = navSystem.CalculatePath(delayedPosition);
            if (pathToDelayedPosition.IsComplete())
            {
                lastKnownPositionPath.Value = pathToDelayedPosition;
                return TaskStatus.Success;
            }
            
            throw new ArgumentException("Impossible to reach the enemy, estimated position in not valid!");
        }

        private TaskStatus EstimateEnemyPositionFromDamage()
        {
            // Estimate enemy position: get enemy position (assuming it's also the position from which 
            // we got damaged. Draw a line between my pos in the direction of the enemy and pick any point
            // in such line (maybe in the second half of the line, otherwise we seek too close). Draw a circle
            // around that point and pick one point inside of it. That's the enemy estimated position.

            var delay = damageSensor.LastTimeDamaged - Time.time;
            var (enemyPos, _) = enemyTracker.GetPositionAndVelocityFromDelay(delay);

            var myPosition = transform.position;
            var direction = enemyPos - myPosition;
            if (Physics.Raycast(myPosition, direction, out var hit, direction.magnitude * 2f))
            {
                var chosenDistance = (0.5f + Random.value * 0.5f) * hit.distance;
                var pointOnLine = myPosition + chosenDistance * direction.normalized;

                // size of radius is 1/3 of the distance, so we avoid looking behind us
                var radiusSize = hit.distance * Random.value * 0.3f;
                var circle = Random.insideUnitCircle * radiusSize;
                var chosenPos = pointOnLine;
                chosenPos.x += circle.x;
                chosenPos.z += circle.y;

                var path = navSystem.CalculatePath(chosenPos);
                if (path.IsComplete())
                {
                    lastKnownPositionPath.Value = path;
                    return TaskStatus.Success;
                }
                // The position chosen is not valid... choose the point we have hit?
                var path2 = navSystem.CalculatePath(hit.point);
                if (path2.IsComplete())
                {
                    lastKnownPositionPath.Value = path2; 
                    return TaskStatus.Success;
                }
                // todo i'd like to understand why is the point unreachable here...
                // ... choose enemy position...
                var path3 = navSystem.CalculatePath(enemyPos);
                if (path3.IsComplete())
                {
                    lastKnownPositionPath.Value = path3;
                    return TaskStatus.Success;
                }
                // Give up on life
                throw new ArgumentException("Cannot get valid path to enemy");
            }
            // How come we haven't spotted anything in the enemy direction? Not even the enemy? Impossible!
            throw new ApplicationException("We couldn't spot the enemy even when raycasting towards it!");
        }
    }
}