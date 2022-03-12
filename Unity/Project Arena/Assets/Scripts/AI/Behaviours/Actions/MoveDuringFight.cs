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
    /// Handles moving during fight. How tactical the movement is (e.g. strifing) depends on the
    /// <see cref="FightingMovementSkill"/>
    /// </summary>
    [Serializable]
    public class MoveDuringFight : Action
    {
        private const int MIN_STRAFE_LENGTH = 15;
        private const int MAX_STRAFE_LENGTH = 40;
        private AIEntity entity;
        private GunManager gunManager;
        private NavigationSystem navSystem;
        private int remainingStrafes = Random.Range(MIN_STRAFE_LENGTH, MAX_STRAFE_LENGTH);
        private FightingMovementSkill skill;

        private bool strifeRight = Random.value < 0.5;
        private Entity.Entity target;
        private const int maxAttempts = 10;

        private int lineCastLayerMask;
        private bool isChasingEnemy;
        public override void OnAwake()
        {
            lineCastLayerMask = ~LayerMask.GetMask("Ignore Raycast", "Entity", "Projectile");
        }

        public override void OnStart()
        {
            entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            gunManager = entity.GunManager;
            target = entity.GetEnemy();
            skill = entity.MovementSkill;
            navSystem.CancelPath();
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (navSystem.HasPath() && !navSystem.HasArrivedToDestination() && !ShouldRecomputePath())
            {
                navSystem.MoveAlongPath();
                return TaskStatus.Running;
            }
            navSystem.CancelPath();

            // We won't interfere raycasts and linecasts with our own presence
            // entity.SetIgnoreRaycast(true);
            TrySelectDestination();
            // entity.SetIgnoreRaycast(false);
            return TaskStatus.Running;
        }

        private bool ShouldRecomputePath()
        {
            if (!isChasingEnemy)
            {
                // Is not chasing enemy, so we do not have anything to check
                return false;
            }

            if (!Physics.Linecast(transform.position, target.transform.position, lineCastLayerMask))
            {
                // We can see the enemy, no longer need to chase
                isChasingEnemy = false;
                return true;
            }
            return false;
        }

        private void TrySelectDestination()
        {
            if (skill == FightingMovementSkill.StandStill)
            {
                // Don't move at all if skill is so low.
                navSystem.CancelPath();
                return;
            }

            var currentPos = transform.position;
            var targetPos = target.transform.position;

            bool canSeeEnemyFromStartingPoint;

            // Check if we can see enemy from where we are
            canSeeEnemyFromStartingPoint = !Physics.Linecast(currentPos, targetPos, lineCastLayerMask);

            // We do not care if enemy is above or below us, the move straight/strife movement should be
            // parallel to the floor.
            targetPos.y = currentPos.y;

            // If we can see the enemy from the starting point, we should play as usual
            if (canSeeEnemyFromStartingPoint)
            {
                SetMoveDestinationWhenEnemyCanBeSeen(currentPos, targetPos);
            }
            else
            {
                MoveToLocationWithEnemyInSight(currentPos, targetPos);
            }
        }

        private void MoveToLocationWithEnemyInSight(Vector3 currentPos, Vector3 targetPos)
        {
            // We cannot see the enemy from where we are, try to find a position from where it is visible.
            // How? For now, random selection (even if this breaks the movement capabilities of the bot)

            // Also reset strife
            strifeRight = Random.value > 0.5f;
            for (var i = 0; i < maxAttempts; i++)
            {
                var randomDir = Random.insideUnitCircle;
                var newPos = currentPos;
                newPos.x += randomDir.x;
                newPos.z += randomDir.y;

                if (!Physics.Linecast(newPos, targetPos, lineCastLayerMask))
                {
                    // Can see enemy from here, found new position!

                    var path = navSystem.CalculatePath(newPos);
                    if (path.IsComplete())
                    {
                        navSystem.SetPath(path);
                        return;
                    }
                }
            }

            // I couldn't see the enemy from any place. This bot is stupid and will rush towards the enemy, thinking
            // that it might be fleeing...
            var enemyPath = navSystem.CalculatePath(targetPos);
            if (!enemyPath.IsComplete())
                Debug.LogError(
                    "Enemy is not reachable! My pos: (" + currentPos.x + " , " + currentPos.y + ", " +
                    currentPos.z + "), enemyPos (" + targetPos.x + " , " + targetPos.y + ", " + targetPos.z + ")"
                );
            
            // TODO you should not get all the way to the enemy position.
            // Instead, you should stop following the path as soon as you can see again the enemy.

            isChasingEnemy = true;
            navSystem.SetPath(enemyPath);
        }

        private void SetMoveDestinationWhenEnemyCanBeSeen(Vector3 currentPos, Vector3 targetPos)
        {
            var unNormalizedDirection = targetPos - currentPos;
            var distance = unNormalizedDirection.magnitude;
            var direction = unNormalizedDirection.normalized;

            var movementDirectionDueToGun = GetMoveDirectionDueToGun(distance, direction);
            var movementDirectionDueToStrife = GetMovementDirectionDueToStrife(direction);

            var totalMovement = movementDirectionDueToStrife + movementDirectionDueToGun * 3f;
            var newPos = currentPos + totalMovement;

            if (!Physics.Linecast(newPos, targetPos, lineCastLayerMask))
            {
                // I can see the enemy from the new position.
                var path = navSystem.CalculatePath(newPos);
                if (!path.IsValid())
                {
                    // TODO I Cannot set the path. Why? I should move somewhere else?
                    Debug.DrawLine(currentPos, newPos, Color.magenta, 2f, false);
                    strifeRight = !strifeRight;
                    // TODO Is this ok?
                    MoveToLocationWithEnemyInSight(currentPos, targetPos);
                }
                else
                    navSystem.SetPath(path);
            }
            else
            {
                // TODO I Cannot set the enemy. I should move somewhere else. Move toward the enemy.
                // However, do not 
                MoveToLocationWithEnemyInSight(currentPos, targetPos);
            }
        }

        private Vector3 GetMovementDirectionDueToStrife(Vector3 direction)
        {
            if (skill < FightingMovementSkill.CircleStrife)
            {
                // Too n00b to strife
                return Vector3.zero;
            }

            var movementDirectionDueToStrife = Vector3.zero;
            var enemyLookDirection = target.transform.forward;
            var lookAngleRelativeToDirection = Vector3.SignedAngle(enemyLookDirection, direction, Vector3.up);
            if (Mathf.Abs(lookAngleRelativeToDirection) > 90)
            {
                // Only strife if enemy is looking
                movementDirectionDueToStrife = Vector3.Cross(direction, transform.up);
                if (skill == FightingMovementSkill.CircleStrifeChangeDirection)
                {
                    remainingStrafes--;
                    if (remainingStrafes < 0)
                    {
                        remainingStrafes = Random.Range(MIN_STRAFE_LENGTH, MAX_STRAFE_LENGTH);
                        strifeRight = !strifeRight;
                    }
                }

                if (!strifeRight) movementDirectionDueToStrife = -movementDirectionDueToStrife;
            }

            return movementDirectionDueToStrife;
        }

        private Vector3 GetMoveDirectionDueToGun(float distance, Vector3 direction)
        {
            Vector3 movementDirectionDueToGun;
            // Get current gun optimal range
            var (closeRange, farRange) = gunManager.GetCurrentGunOptimalRange();
            if (distance < closeRange)
                movementDirectionDueToGun = -direction;
            else if (distance > farRange)
                movementDirectionDueToGun = direction;
            else
                // Randomly move through the optimal range of the gun, but not too much
                // TODO Perhaps have a flag like strife to avoid oscillating too much
                movementDirectionDueToGun = direction * (Random.value > 0.5f ? -0.1f : 0.1f);
            return movementDirectionDueToGun;
        }
    }
}