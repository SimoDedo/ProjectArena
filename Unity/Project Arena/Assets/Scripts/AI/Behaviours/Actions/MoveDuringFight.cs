using System;
using System.Linq;
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
        private Entity.Entity target;
        private Transform targetTransform;

        private bool isStrafing;
        private bool isStrafingRight;
        private float nextStrafeChangeTime;

        private float timeStuckInCorner = 0;
        private bool isGettingUnstuck = false;
        
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
            if (isGettingUnstuck)
            {
                navSystem.MoveAlongPath();
                isGettingUnstuck = !navSystem.HasArrivedToDestination();
                return TaskStatus.Running;
            }
            
            var currentPos = entityTransform.position;
            var targetPos = targetTransform.position;
            if (!Physics.Linecast(currentPos, targetPos, layerMask))
            {
                if (TacticalMovement(currentPos, targetPos))
                {
                    // navSystem.MoveAlongPath();
                    timeStuckInCorner = Math.Max(0, timeStuckInCorner - Time.deltaTime * 0.1f);
                    return TaskStatus.Running;
                }
                
                if ((currentPos - targetPos).magnitude < 14f)
                {
                    // We failed the tactical movement and we are very close to the enemy. Likely we are stuck in a corner.
                    timeStuckInCorner += Time.deltaTime;
                    if (timeStuckInCorner > 0.15)
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            // Compute path 
                            var randomCircle = Random.insideUnitCircle;
                            var randomDisplacementVector = new Vector3(randomCircle.x, randomCircle.y) * 15f;
                            var newDestination = currentPos + randomDisplacementVector;
                            var path = navSystem.CalculatePath(newDestination);
                            if (!path.IsValid()) continue;
                            isGettingUnstuck = true;
                            timeStuckInCorner = 0;
                            navSystem.SetPath(path);
                            navSystem.MoveAlongPath();
                            return TaskStatus.Running;
                        }
                    }
                }
            }

            // If we get here, either we cannot see the enemy or we cannot compute a valid path reaching it.
            // Try random positions, hoping to find a suitable location.
            FallbackMovement(currentPos, targetPos);

            // navSystem.MoveAlongPath();
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

            // For gun MM of 1, I want 100% movement to be for the gun for skill = 0 and 80% for skill = 1
            // For gun MM of 0.3, I want 100% movement to be for the gun for skill = 0 and 10% for skill = 1
            // Desired gun movement weight = 1 - skill * ( - 1 * gun magnitude + 1.2)
            // Strafe magnitude must be sqrt( (GM - GW)^2 - GW^2)
            
            // TODO if I don't strafe, the entity moves slower.
            var gunMovement = GetMoveDirectionDueToGunRange(distance, direction);
            var strafeMovement = GetMovementDirectionDueToStrafe(direction);

            Vector3 movementVector;
            if (strafeMovement != Vector3.zero)
            {
                var gunMagnitude = gunMovement.magnitude;
                var gunPercentage = Mathf.Pow(1 - skill * (-6 / 7f * gunMagnitude + 0.04f + 6 / 7f), 2);
                var strafeMagnitude = Mathf.Sqrt(
                    Mathf.Pow(gunMagnitude / gunPercentage, 2) - Mathf.Pow(gunPercentage, 2)
                );
                movementVector = gunMovement + strafeMovement * strafeMagnitude;
            }
            else
            {
                movementVector = gunMovement;
            }
            
            // Debug.Log("AAA gun movement for " + entity.GetID() + " is " + 
            //           (gunMovement.magnitude / movementVector.magnitude) + ", " +
            //           Mathf.Pow(gunMovement.magnitude / movementVector.magnitude, 2) + 
            //           "of total, while strafe is " + 
            //           (strafeMovement.magnitude / movementVector.magnitude) + ", " + 
            //           Mathf.Pow(strafeMovement.magnitude / movementVector.magnitude, 2)
            // );

            var totalMovement = movementVector.normalized * Time.deltaTime * navSystem.Speed;
            
            // Debug.Log("AAAA movement for " + entity.GetID() + " is speed " + (Time.deltaTime * navSystem.Speed));
            // Debug.Log("AAAA movement for " + entity.GetID() + ", magn: " + totalMovement.magnitude);
            
            var positionAfterMovement = currentPos + totalMovement;
            // Debug.Log("AAAA movement for " + entity.GetID() + ", position after: " + positionAfterMovement);

            Debug.DrawLine(currentPos, positionAfterMovement, Color.yellow, 0, false);

            if (!Physics.Linecast(positionAfterMovement, targetPos, layerMask))
            {
                // I can see the position, try to compute path!
                path = navSystem.CalculatePath(positionAfterMovement);
                if (path.IsComplete())
                {
                    // Debug.Log("AAAA movement for " + entity.GetID() + ", setting path as: " + path.corners.Last());
                    // navSystem.SetPath(path);
                    navSystem.MoveTo(positionAfterMovement);
                    return true;
                }

                // Path is invalid, draw things to understand why.
                // Debug.DrawLine(currentPos, positionAfterMovement, Color.magenta, 2f, false);
                // MoveToLocationWithEnemyInSight(currentPos, targetPos);
            }

            UpdateStrafeIfNeeded(true);
            // isStrafingRight = !isStrafingRight;

            // Enemy cannot be seen from new position or new position is invalid. Ignore strife
            positionAfterMovement = currentPos + gunMovement * Time.deltaTime * navSystem.Speed;

            // It wouldn't make sense for me to be unable to see the enemy from this new position, given that I moved
            // along the line connecting me and the enemy, so I'll not check
            path = navSystem.CalculatePath(positionAfterMovement);

            if (!path.IsComplete()) return false; // Cannot move to new position at all. Try something else.
            // navSystem.SetPath(path);
            navSystem.MoveTo(positionAfterMovement);
            return true;
        }

        private void FallbackMovement(Vector3 currentPos, Vector3 targetPos)
        {
            Vector3 validPos = Vector3.zero;

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
                validPos = newPos;
                    
                if (Physics.Linecast(newPos, targetPos, layerMask)) continue;
                    
                // Can see enemy from here, found new position!
                // navSystem.SetPath(path);
                navSystem.MoveTo(newPos);
                return;
            }

            if (validPos != Vector3.zero)
            {
                navSystem.MoveTo(validPos);
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

        private Vector3 GetMoveDirectionDueToGunRange(float distance, Vector3 direction)
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
            if (!force && !(nextStrafeChangeTime <= Time.time)) return;
            nextStrafeChangeTime = Time.time + Random.Range(0.3f, 0.5f);
            isStrafing = Random.value < skill;
            isStrafingRight ^= (Random.value < 0.8f);
        }
    }
}
