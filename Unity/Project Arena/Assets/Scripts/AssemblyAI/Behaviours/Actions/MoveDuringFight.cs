using System;
using System.Linq;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime.Tasks;
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
        private static readonly Vector3 NoDestination = new Vector3(10000, 10000, 10000);
        private Vector3 destination;
        private FightingMovementSkill skill;

        public override void OnStart()
        {
            entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            gunManager = entity.GunManager;
            target = entity.GetEnemy();
            skill = entity.MovementSkill;
            navSystem.CancelPath();
            destination = NoDestination;
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (destination != NoDestination && !navSystem.HasArrivedToDestination(destination))
            {
                // Call every frame, just in case someone overwrote our destination choice (e.g. to avoid rocket)
                navSystem.SetDestination(destination);
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
                else if (distance > farRange) movementDirectionDueToGun = direction;
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
                Debug.DrawLine(newPos, targetPos, Color.blue);

                if (!Physics.Linecast(newPos, targetPos, out var hit) ||
                    hit.collider.gameObject == target.gameObject)
                {
                    var path = navSystem.CalculatePath(newPos);
                    if (path.status != NavMeshPathStatus.PathComplete)
                        strifeRight = !strifeRight;
                    else
                    {
                        destination = path.corners.Last();
                        navSystem.SetPathToDestination(path);
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
                        destination = newPos;
                        if (navSystem.SetDestination(newPos))
                        {
                            // Destination is valid!                                
                            return;
                        }
                    }
                }

                // I couldn't see the enemy from any place. This bot is stupid and will rush towards the enemy, thinking
                // that it might be fleeing...
                navSystem.SetDestination(targetPos);
                //... however it should do this only in this frame, not until it has reached the enemy.
                destination = NoDestination;
            }
        }
    }
}