using System;
using System.Linq;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime.Tasks;
using Others;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class MoveDuringFight : Action
    {
        private AIEntity entity;
        private NavigationSystem navSystem;
        private GunManager gunManager;
        private Entity target;

        private bool strifeRight = Random.value < 0.5;
        private int remainingStrafes = Random.Range(MIN_STRAFE_LENGTH, MAX_STRAFE_LENGTH);

        private const int MIN_STRAFE_LENGTH = 10;
        private const int MAX_STRAFE_LENGTH = 30;
        private FightingMovementSkill skill;

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
            if (navSystem.HasPath() && !navSystem.HasArrivedToDestination())
            {
                navSystem.MoveAlongPath();
                return TaskStatus.Running;
            }

            // We won't interfere raycasts and linecasts with our own presence
            entity.SetIgnoreRaycast(true);
            TrySelectDestination();
            entity.SetIgnoreRaycast(false);
            return TaskStatus.Running;
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
            canSeeEnemyFromStartingPoint = !Physics.Linecast(currentPos, targetPos, out var canSeeEnemyHit) ||
                canSeeEnemyHit.collider.gameObject == target.gameObject;

            // We do not care if enemy is above or below us, the move straight/strife movement should be
            // parallel to the floor.
            targetPos.y = currentPos.y;

            var unNormalizedDirection = targetPos - currentPos;
            var distance = unNormalizedDirection.magnitude;
            var direction = unNormalizedDirection.normalized;

            // If we can see the enemy from the starting point, we should play as usual
            if (canSeeEnemyFromStartingPoint)
            {
                Vector3 movementDirectionDueToGun;
                var movementDirectionDueToStrife = Vector3.zero;

                // Get current gun optimal range
                var (closeRange, farRange) = gunManager.GetCurrentGunOptimalRange();
                if (distance < closeRange)
                    movementDirectionDueToGun = -direction;
                else if (distance > farRange)
                    movementDirectionDueToGun = direction;
                else
                {
                    // Randomly move through the optimal range of the gun, but not too much
                    movementDirectionDueToGun = direction * (Random.value > 0.5f ? -0.3f : 0.3f);
                }

                if (skill >= FightingMovementSkill.CircleStrife)
                {
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
                }

                var totalMovement = movementDirectionDueToStrife + movementDirectionDueToGun * 3f;
                var newPos = currentPos + totalMovement;

                if (!Physics.Linecast(newPos, targetPos, out var hit) ||
                    hit.collider.gameObject == target.gameObject)
                {
                    var path = navSystem.CalculatePath(newPos);
                    if (path.status != NavMeshPathStatus.PathComplete)
                        strifeRight = !strifeRight;
                    else
                    {
                        navSystem.SetPath(path);
                    }
                }
                else
                {
                    strifeRight = !strifeRight;
                }
            }
            else
            {
                // We cannot see the enemy from where we are, try to find a position from where it is visible.
                // How? For now, random selection (even if this breaks the movement capabilities of the bot)

                // Also reset strife
                strifeRight = Random.value > 0.5f;
                const int maxAttempts = 10;
                for (var i = 0; i < maxAttempts; i++)
                {
                    var randomDir = Random.insideUnitCircle;
                    var newPos = currentPos;
                    newPos.x += randomDir.x;
                    newPos.z += randomDir.y;

                    if (!Physics.Linecast(newPos, targetPos, out var finalPosVisibility)
                        || finalPosVisibility.collider.gameObject == target.gameObject)
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
                {
                    Debug.LogError(
                        "Enemy is not reachable! My pos: (" + currentPos.x + " , " + currentPos.y + ", " +
                        currentPos.z + "), enemyPos (" + targetPos.x + " , " + targetPos.y + ", " + targetPos.z + ")"
                    );
                }

                navSystem.SetPath(enemyPath);
            }
        }
    }
}