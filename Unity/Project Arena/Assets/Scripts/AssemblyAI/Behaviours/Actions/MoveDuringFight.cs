using System;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
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
        private int remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);

        private const int minStrifeLength = 10;
        private const int maxStrifeLength = 30;
        private FightingMovementSkill skill;
        private Collider[] rocketTestCollider = new Collider[4];
        private const float ROCKET_DETECTION_RADIUS = 40f;

        public override void OnStart()
        {
            navSystem = GetComponent<NavigationSystem>();
            entity = GetComponent<AIEntity>();
            gunManager = GetComponent<GunManager>();
            target = entity.GetEnemy();
            skill = entity.GetMovementSkill();
            navSystem.CancelPath();
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            // Is able enough to dodge rockets?
            if (skill >= FightingMovementSkill.CircleStrife)
            {
                var rocketToDodge = FindRocketToDodge();
                if (rocketToDodge != null)
                {
                    DodgeRocket(rocketToDodge);
                    return TaskStatus.Running;
                }
            }

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

        private void DodgeRocket(Projectile rocketToDodge)
        {
            // This rocket is not mine... How do I dodge it?
            // Calculate it's trajectory and try to get away from it

            var projectileTransform = rocketToDodge.transform;
            var projectilePosition = projectileTransform.position;
            var projectileDirection = projectileTransform.forward;

            var myDirection = transform.position - projectilePosition;
            var up = transform.up;
            var angle = Vector3.SignedAngle(myDirection, projectileDirection, up);

            // Try to strife in direction that increases this angle
            var avoidDirection = Vector3.Cross(up, myDirection).normalized;
            if (angle > 0f)
                avoidDirection = -avoidDirection;
            navSystem.SetDestination(transform.position + avoidDirection * navSystem.GetSpeed());
            navSystem.MoveAlongPath();
        }

        private Projectile FindRocketToDodge()
        {
            Projectile projectileToDodge = null;
            if (skill >= FightingMovementSkill.CircleStrife)
            {
                // Detect rocket presence
                var position = transform.position;
                var projectileColliders = Physics.OverlapSphereNonAlloc(position, ROCKET_DETECTION_RADIUS,
                    rocketTestCollider,
                    1 << LayerMask.NameToLayer("Projectile"));

                var closestRocket = float.MaxValue;

                if (projectileColliders != 0)
                {
                    // Check if any projectile doesn't belong to me
                    for (var i = 0; i < projectileColliders; i++)
                    {
                        var projectile = rocketTestCollider[i].GetComponent<Projectile>();
                        if (projectile == null) continue;
                        if (projectile.ShooterID != entity.GetID())
                        {
                            // This rocket is not mine... How do I dodge it?
                            // Calculate it's trajectory and try to get away from it

                            var projectileTransform = projectile.transform;
                            var projectilePosition = projectileTransform.position;
                            var distance = (position - projectilePosition).sqrMagnitude;
                            if (distance < closestRocket)
                            {
                                // calculate angle, ignore projectiles that are going in the opposite direction
                                var projectileDirection = projectileTransform.forward;
                                var myDirection = position - projectilePosition;

                                if (Vector3.Angle(myDirection, projectileDirection) < 45)
                                {
                                    closestRocket = distance;
                                    projectileToDodge = projectile;
                                }
                            }
                        }
                    }
                }
            }

            return projectileToDodge;
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
                else if (distance > farRange)
                    movementDirectionDueToGun = direction;

                if (skill >= FightingMovementSkill.CircleStrife)
                {
                    movementDirectionDueToStrife = Vector3.Cross(direction, transform.up);
                    if (skill == FightingMovementSkill.CircleStrifeChangeDirection)
                    {
                        remainingStrifes--;
                        if (remainingStrifes < 0)
                        {
                            remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);
                            strifeRight = !strifeRight;
                        }
                    }

                    if (!strifeRight)
                        movementDirectionDueToStrife = -movementDirectionDueToStrife;
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
                        navSystem.SetPath(path);
                        navSystem.MoveAlongPath();
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
                            navSystem.SetDestination(finalPos);
                            navSystem.MoveAlongPath();
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