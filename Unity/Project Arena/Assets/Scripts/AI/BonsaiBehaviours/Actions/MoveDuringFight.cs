using System;
using AI.Layers.Actuators;
using AI.Layers.KnowledgeBase;
using Bonsai;
using Bonsai.Core;
using Entity.Component;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// TODO
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class MoveDuringFight : Task
    {
        private const int maxAttempts = 10;
        private AIEntity entity;
        private Transform entityTransform;

        private GunManager gunManager;

        private int layerMask;
        private NavigationSystem navSystem;
        private MovementController mover;

        private float skill;
        private Entity.Entity target;
        private Transform targetTransform;

        private bool isStrafing;
        private bool isStrafingRight;
        private float nextStrafeChangeTime;

        private float timeStuckInCorner;

        public override void OnStart()
        {
            layerMask = LayerMask.GetMask("Default", "Wall", "Floor");
            entity = Actor.GetComponent<AIEntity>();
            entityTransform = entity.transform;
            navSystem = entity.NavigationSystem;
            mover = entity.MovementController;            
            gunManager = entity.GunManager;
            target = entity.GetEnemy();
            targetTransform = target.transform;

            skill = entity.Characteristics.FightingSkill;
            isStrafingRight = Random.value < 0.5f;
        }

        public override void OnEnter()
        {
            UpdateStrafeIfNeeded(true);
            navSystem.CancelPath();
        }

        public override void OnExit()
        {
            navSystem.CancelPath();
        }

        public override Status Run()
        {
            if (navSystem.HasPath() && !navSystem.HasArrivedToDestination())
            {
                mover.MoveToPosition(navSystem.GetNextPosition());
                return Status.Running;
            }
            
            var currentPos = entityTransform.position;
            var targetPos = targetTransform.position;
            if (!Physics.Linecast(currentPos, targetPos, layerMask))
            {
                if (TacticalMovement(currentPos, targetPos))
                {
                    mover.MoveToPosition(navSystem.GetNextPosition());
                    timeStuckInCorner = Math.Max(0, timeStuckInCorner - Time.deltaTime * 0.1f);
                    return Status.Running;
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
                            timeStuckInCorner = 0;
                            navSystem.SetPath(path);
                            mover.MoveToPosition(navSystem.GetNextPosition());
                            return Status.Running;
                        }
                    }
                }
            }

            // If we get here, either we cannot see the enemy or we cannot compute a valid path reaching it.
            // Try random positions, hoping to find a suitable location.
            FallbackMovement(currentPos, targetPos);

            // mover.MoveToPosition(navSystem.GetNextPosition());
            return Status.Running;
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
            // Formula used, where X represents magnitude of gun MM and Y strafe MM:
            // Y^2 / (X^2 + Y^2) = 0.2 * X^2 + 0.77 * skill * (1 - X^2)
            var gunMovement = GetMoveDirectionDueToGunRange(distance, direction);
            var strafeMovement = GetMovementDirectionDueToStrafe(direction);

            Vector3 movementVector;
            if (strafeMovement != Vector3.zero)
            {
                var gunMagnitude = gunMovement.sqrMagnitude;

                var strafeMagnitude = Mathf.Sqrt( 
                    Mathf.Max(
                        skill * gunMagnitude * (97f - 77f*gunMagnitude) / (skill * (77f * gunMagnitude - 97f) + 100f)
                    )
                );
                strafeMovement *= strafeMagnitude;
                movementVector = gunMovement + strafeMovement;
                movementVector.Normalize();
            }
            else
            {
                movementVector = gunMovement;
            }

            var totalMovement = movementVector * (0.3f * navSystem.Speed);
            var positionAfterMovement = currentPos + totalMovement;
            // Debug.DrawLine(currentPos, positionAfterMovement, Color.yellow, 0, false);

            if (!Physics.Linecast(positionAfterMovement, targetPos, layerMask))
            {
                // I can see the position, try to compute path!
                path = navSystem.CalculatePath(positionAfterMovement);
                if (path.IsValid())
                {
                    // Debug.Log("AAAA movement for " + entity.GetID() + ", setting path as: " + path.corners.Last());
                    navSystem.SetPath(path);
                    // navSystem.MoveTo(positionAfterMovement);
                    return true;
                }

                // Path is invalid, draw things to understand why.
                // Debug.DrawLine(currentPos, positionAfterMovement, Color.magenta, 2f, false);
                // MoveToLocationWithEnemyInSight(currentPos, targetPos);
            }

            // UpdateStrafeIfNeeded(true);
            // isStrafingRight = !isStrafingRight;

            // Enemy cannot be seen from new position or new position is invalid. Ignore strife
            positionAfterMovement = currentPos + gunMovement * (0.3f * navSystem.Speed);

            // It wouldn't make sense for me to be unable to see the enemy from this new position, given that I moved
            // along the line connecting me and the enemy, so I'll not check
            path = navSystem.CalculatePath(positionAfterMovement);

            if (!path.IsValid()) return false; // Cannot move to new position at all. Try something else.
            navSystem.SetPath(path);
            // navSystem.MoveTo(positionAfterMovement);
            return true;
        }

        private void FallbackMovement(Vector3 currentPos, Vector3 targetPos)
        {
            // We cannot see the enemy from where we are, try to find a position from where it is visible.
            // UpdateStrafeIfNeeded(true);
            NavMeshPath path;
            for (var i = 0; i < maxAttempts; i++)
            {
                var randomDir = Random.insideUnitCircle;
                var newPos = currentPos;
                newPos.x += randomDir.x;
                newPos.z += randomDir.y;

                path = navSystem.CalculatePath(newPos);
                if (!path.IsValid()) continue;

                if (Physics.Linecast(newPos, targetPos, layerMask)) continue;

                // Can see enemy from here, found new position!
                navSystem.SetPath(path);
                mover.MoveToPosition(navSystem.GetNextPosition());
                return;
            }

            var distance = (targetPos - currentPos).magnitude;
            var (minDistance, _) = gunManager.GetCurrentGunOptimalRange();
            if (distance <= minDistance) return;

            // We can try to move closer to the enemy
            path = navSystem.CalculatePath(targetPos);
            if (!path.IsValid())
            {
                throw new Exception("AAA");
            }

            var currentPosition = currentPos;
            var totalDistance = 0f;
            foreach (var corner in path.corners)
            {
                totalDistance += (corner - currentPosition).magnitude;
                currentPosition = corner;
                // If we move less than two seconds, extend path
                if (totalDistance / 20 < 2) continue;
                navSystem.CalculatePath(corner);
                mover.MoveToPosition(navSystem.GetNextPosition());
                return;
            }
            navSystem.SetPath(path);
            mover.MoveToPosition(navSystem.GetNextPosition());
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
            nextStrafeChangeTime = Time.time + Random.Range(0.7f, 1.1f);
            isStrafing = Random.value < skill;
            isStrafingRight ^= (Random.value < 0.8f);
        }
    }
}
