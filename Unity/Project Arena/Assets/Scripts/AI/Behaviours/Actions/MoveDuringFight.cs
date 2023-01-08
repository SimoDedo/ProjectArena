using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime.Tasks;
using Entity.Component;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// TODO
    /// </summary>
    [Serializable]
    public class MoveDuringFight : Action
    {
        private const int maxAttempts = 10;
        private AIEntity entity;
        private Transform entityTransform;

        private GunManager gunManager;

        private int layerMask;
        private NavigationSystem navSystem;

        private float skill;
        private float strifeMovementWeight;
        private Entity.Entity target;
        private Transform targetTransform;

        private bool isStrafing;
        private bool isStrafingRight;
        private float nextStrafeChangeTime;

        public override void OnAwake()
        {
            layerMask = LayerMask.GetMask("Default", "Wall", "Floor");
        }

        public override void OnStart()
        {
            entity = GetComponent<AIEntity>();
            entityTransform = entity.transform;
            navSystem = entity.NavigationSystem;
            gunManager = entity.GunManager;
            target = entity.GetEnemy();
            targetTransform = target.transform;

            skill = entity.Characteristics.FightingSkill;
            strifeMovementWeight = 0.5f * skill * 0.5f;
            isStrafing = Random.value < skill;
            isStrafingRight = Random.value < 0.5f;
            UpdateStrafeIfNeeded(true);
            navSystem.CancelPath();
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            var currentPos = entityTransform.position;
            var targetPos = targetTransform.position;
            if (!Physics.Linecast(currentPos, targetPos, layerMask))
            {
                if (TacticalMovement(currentPos, targetPos))
                {
                    navSystem.MoveAlongPath();
                    return TaskStatus.Running;
                }
            }

            // If we get here, either we cannot see the enemy or we cannot compute a valid path reaching it.
            // Try random positions, hoping to find a suitable location.
            FallbackMovement(currentPos, targetPos);

            navSystem.MoveAlongPath();
            return TaskStatus.Running;
        }

        private bool TacticalMovement(Vector3 currentPos, Vector3 targetPos)
        {
            NavMeshPath path;
            // We do not care if enemy is above or below us, the move straight/strife movement should be
            // parallel to the floor.
            var sameHeightEnemyPos = new Vector3(targetPos.x, currentPos.y, targetPos.z);
            
            var unNormalizedDirection = sameHeightEnemyPos - currentPos;
            var distance = unNormalizedDirection.magnitude;
            var direction = unNormalizedDirection.normalized;

            var movementDirectionDueToGun = GetMoveDirectionDueToGun(distance, direction);
            var movementDirectionDueToStrife = GetMovementDirectionDueToStrafe(direction);

            // x5 multiplier to avoid entity moving too slowly
            var totalMovement = (movementDirectionDueToGun + strifeMovementWeight * movementDirectionDueToStrife) *
                                Time.deltaTime * navSystem.Speed * 5f; 
            
            var positionAfterMovement = currentPos + totalMovement;
            
            Debug.DrawLine(currentPos, positionAfterMovement, Color.yellow, 0, false);

            if (!Physics.Linecast(positionAfterMovement, targetPos, layerMask))
            {
                // I can see the position, try to compute path!
                path = navSystem.CalculatePath(positionAfterMovement);
                if (path.IsValid())
                {
                    navSystem.SetPath(path);
                    return true;
                }

                // Path is invalid, draw things to understand why.
                // Debug.DrawLine(currentPos, positionAfterMovement, Color.magenta, 2f, false);
                // MoveToLocationWithEnemyInSight(currentPos, targetPos);
            }

            // Enemy cannot be seen from new position or new position is invalid. Ignore strife
            positionAfterMovement = movementDirectionDueToGun * Time.deltaTime * navSystem.Speed;

            // It wouldn't make sense for me to be unable to see the enemy from this new position, given that I moved
            // along the line connecting me and the enemy, so I'll not check
            path = navSystem.CalculatePath(positionAfterMovement);

            if (!path.IsValid()) return false; // Cannot move to new position at all. Try something else.
            navSystem.SetPath(path);
            return true;
        }

        private void FallbackMovement(Vector3 currentPos, Vector3 targetPos)
        {
            NavMeshPath validPath = null;

            // We cannot see the enemy from where we are, try to find a position from where it is visible.

            UpdateStrafeIfNeeded(true);
            for (var i = 0; i < maxAttempts; i++)
            {
                var randomDir = Random.insideUnitCircle;
                var newPos = currentPos;
                newPos.x += randomDir.x;
                newPos.z += randomDir.y;

                var path = navSystem.CalculatePath(newPos);
                if (!path.IsComplete()) continue;
                
                // Let's store one of the paths tried; in case the enemy is not visible from any position,
                // at least we have somewhere to move.
                validPath = path;
                    
                if (Physics.Linecast(newPos, targetPos, layerMask)) continue;
                    
                // Can see enemy from here, found new position!
                navSystem.SetPath(path);
                return;
            }

            if (validPath != null)
            {
                navSystem.SetPath(validPath);
            }
        }

        private Vector3 GetMovementDirectionDueToStrafe(Vector3 direction)
        {
            UpdateStrafeIfNeeded();
            if (!isStrafing) return Vector3.zero;
            var enemyLookDirection = targetTransform.forward;
            var lookAngleRelativeToDirection = Vector3.SignedAngle(enemyLookDirection, direction, Vector3.up);
            if (Mathf.Abs(lookAngleRelativeToDirection) <= 90)
            {
                // Only strafe if enemy is looking us
                return Vector3.zero;
            }
            
            return isStrafingRight ? 
                Vector3.Cross(direction, entityTransform.up) : 
                Vector3.Cross(direction, -entityTransform.up);
        }

        private Vector3 GetMoveDirectionDueToGun(float distance, Vector3 direction)
        {
            Vector3 movementDirectionDueToGun;
            // Get current gun optimal range
            var (closeRange, farRange) = gunManager.GetCurrentGunOptimalRange();
            var rangeSize = farRange - closeRange;

            var skillAdjustedCloseRange = closeRange + rangeSize * 
                entity.Characteristics.GunMovementCorrectness / 100;
            var skillAdjustedFarRange = farRange + rangeSize * 
                entity.Characteristics.GunMovementCorrectness / 100;

            if (distance < skillAdjustedCloseRange)
            {
                var adjustment = Math.Min(1.0f, skillAdjustedCloseRange - distance);
                movementDirectionDueToGun = -(direction * adjustment);
            }
            else if (distance > skillAdjustedFarRange)
            {
                var adjustment = Math.Min(1.0f, distance - skillAdjustedFarRange);
                movementDirectionDueToGun = direction * adjustment;
            }
            else
                // Randomly move through the optimal range of the gun, but not too much
                // TODO Perhaps have a flag like strafe to avoid oscillating too much
                movementDirectionDueToGun = direction * (Random.value > 0.5f ? -0.3f : 0.3f);

            return movementDirectionDueToGun;
        }

        private void UpdateStrafeIfNeeded(bool force = false)
        {
            if (force || nextStrafeChangeTime <= Time.time)
            {
                nextStrafeChangeTime = Time.time + Random.Range(0.3f, 0.5f);
                isStrafing = Random.value < (skill * 0.8f);
                isStrafingRight = isStrafing ^ (Random.value < 0.1f);
            }        
        }
    }
}
