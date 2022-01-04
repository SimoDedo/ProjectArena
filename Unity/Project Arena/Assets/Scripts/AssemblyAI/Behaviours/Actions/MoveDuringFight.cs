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
        private int remainingStrifes = Random.Range(MIN_STRIFE_LENGTH, MAX_STRIFE_LENGTH);

        private const int MIN_STRIFE_LENGTH = 10;
        private const int MAX_STRIFE_LENGTH = 30;
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
            destination = Vector3.positiveInfinity;
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (destination != Vector3.positiveInfinity && navSystem.HasArrivedToDestination(destination))
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
            // Don't move at all during shooting
            if (skill == FightingMovementSkill.StandStill) return;

            var currentPos = transform.position;
            var targetPos = target.transform.position;

            bool canSeeEnemyFromStartingPoint;

            // Check if we can see enemy from where we are
            canSeeEnemyFromStartingPoint = !Physics.Linecast(currentPos, targetPos, out var canSeeEnemyHit) ||
                canSeeEnemyHit.collider.gameObject == target.gameObject;

            // We do not care if enemy is above or beyond us, the move straight/strife movement should be
            // parallel to the floor.
            targetPos.y = currentPos.y;

            var unNormalizedDirection = targetPos - currentPos;
            var distance = unNormalizedDirection.magnitude;
            var direction = unNormalizedDirection.normalized;

            // If we can see the enemy from the starting point, we should play as usual
            if (canSeeEnemyFromStartingPoint)
            {
                var movementDirectionDueToGun = Vector3.zero;
                var movementDirectionDueToStrife = Vector3.zero;

                // Get current gun optimal range
                var (closeRange, farRange) = gunManager.GetCurrentGunOptimalRange();
                if (distance < closeRange)
                    movementDirectionDueToGun = -direction;
                else if (distance > farRange) movementDirectionDueToGun = direction;

                if (skill >= FightingMovementSkill.CircleStrife)
                {
                    movementDirectionDueToStrife = Vector3.Cross(direction, transform.up);
                    if (skill == FightingMovementSkill.CircleStrifeChangeDirection)
                    {
                        remainingStrifes--;
                        if (remainingStrifes < 0)
                        {
                            remainingStrifes = Random.Range(MIN_STRIFE_LENGTH, MAX_STRIFE_LENGTH);
                            strifeRight = !strifeRight;
                        }
                    }

                    if (!strifeRight) movementDirectionDueToStrife = -movementDirectionDueToStrife;
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
                    newPos.x = randomDir.x;
                    newPos.z = randomDir.y;

                    if (navSystem.IsPointOnNavMesh(newPos, out var finalPos))
                    {
                        if (!Physics.Linecast(finalPos, targetPos, out var finalPosVisibility)
                            || finalPosVisibility.collider.gameObject == target.gameObject)
                        {
                            // Can see enemy from here, found new position!
                            destination = finalPos;
                            navSystem.SetDestination(finalPos);
                            return;
                        }
                    }
                }

                // I couldn't find the enemy from any place. Let's just not move? Otherwise, let's move towards the
                // enemy
            }
        }
    }
}