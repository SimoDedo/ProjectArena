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
        private const int MIN_STRAFE_LENGTH = 40;
        private const int MAX_STRAFE_LENGTH = 200;
        private AIEntity entity;
        private GunManager gunManager;
        private NavigationSystem navSystem;
        private int remainingStrafes = Random.Range(MIN_STRAFE_LENGTH, MAX_STRAFE_LENGTH);

        private float standStillProbability;
        private float randomlyMoveProbability;
        private float nextUpdateTime;
        private bool updateOnlyDueToTimeout;
        private float UPDATE_PERIOD = 0.5f;

        private Recklessness recklessness;

        private bool strifeRight = Random.value < 0.5;
        private Entity.Entity target;
        private const int maxAttempts = 10;

        private int layerMask;

        public override void OnAwake()
        {
            layerMask = LayerMask.GetMask("Default", "Wall");
        }

        public override void OnStart()
        {
            entity = GetComponent<AIEntity>();
            navSystem = entity.NavigationSystem;
            gunManager = entity.GunManager;
            target = entity.GetEnemy();
            randomlyMoveProbability = entity.RandomlyMoveInFightProbability;
            standStillProbability = entity.StandStillInFightProbability;
            recklessness = entity.Recklessness;
            navSystem.CancelPath();
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            // if (navSystem.HasPath() && !navSystem.HasArrivedToDestination())
            // {
            //     navSystem.MoveAlongPath();
            //     return TaskStatus.Running;
            // }
            // navSystem.CancelPath();
            if (updateOnlyDueToTimeout && Time.time >= nextUpdateTime || !updateOnlyDueToTimeout && navSystem.HasArrivedToDestination())
            {
                TrySelectDestination();
            }

            navSystem.MoveAlongPath();
            return TaskStatus.Running;
        }

        private void TrySelectDestination()
        {
            // TODO If I cannot see the enemy I should rush?

            var currentPos = transform.position;

            if (Random.value < standStillProbability)
            {
                // We have to stand still for the next updateTime seconds
                navSystem.CancelPath();
                nextUpdateTime = Time.time + UPDATE_PERIOD;
                updateOnlyDueToTimeout = true;
                return;
            }

            if (Random.value < randomlyMoveProbability)
            {
                var randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0f;
                randomDirection.Normalize();

                for (var attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var finalPosition = currentPos + randomDirection * navSystem.Speed * UPDATE_PERIOD;
                    var path = navSystem.CalculatePath(finalPosition);
                    if (!path.IsValid()) continue;
                    navSystem.SetPath(path);
                    updateOnlyDueToTimeout = false;
                    break;
                }
                return;
            }

            var targetPos = target.transform.position;
            // We do not care if enemy is above or below us, the move straight/strife movement should be
            // parallel to the floor.
            targetPos.y = currentPos.y;
            SetMoveDestinationWhenEnemyCanBeSeen(currentPos, targetPos);
        }

        private void SetMoveDestinationWhenEnemyCanBeSeen(Vector3 currentPos, Vector3 targetPos)
        {
            var unNormalizedDirection = targetPos - currentPos;
            var distance = unNormalizedDirection.magnitude;
            var direction = unNormalizedDirection.normalized;

            var movementDirectionDueToGun = GetMoveDirectionDueToGun(distance, direction);
            var movementDirectionDueToStrife = GetMovementDirectionDueToStrife(direction);

            var totalMovement = movementDirectionDueToStrife + movementDirectionDueToGun;
            var newPos = currentPos + totalMovement * navSystem.Speed * 0.2f;

            Debug.DrawLine(currentPos, newPos, Color.yellow, 1, false);
            // Debug.DrawLine(newPos, targetPos, Color.red, 1, false);

            if (!Physics.Linecast(newPos, targetPos, layerMask))
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
                {
                    updateOnlyDueToTimeout = false;
                    navSystem.SetPath(path);
                }
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

            // TODO Move towards enemy visible with probability depending on recklessness

            var probabilityToMoveWhereSeeEnemy = recklessness switch
            {
                Recklessness.Low => 0.3f,
                Recklessness.Neutral => 0.6f,
                Recklessness.High => 0.9f,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (probabilityToMoveWhereSeeEnemy < Random.value)
            {
                // Also reset strife
                for (var i = 0; i < maxAttempts; i++)
                {
                    var randomDir = Random.insideUnitCircle;
                    var newPos = currentPos;
                    newPos.x += randomDir.x;
                    newPos.z += randomDir.y;

                    if (!Physics.Linecast(newPos, targetPos, layerMask))
                    {
                        // Can see enemy from here, found new position!
                        var path = navSystem.CalculatePath(newPos);
                        if (path.IsComplete())
                        {
                            updateOnlyDueToTimeout = false;
                            navSystem.SetPath(path);
                            return;
                        }
                    }
                }
            }

            // I couldn't see the enemy from any place. Rush towards the enemy, it might be fleeing...
            RushTowardsEnemy(currentPos, targetPos);
        }

        // TODO This will fail if the enemy is on another floor
        private void RushTowardsEnemy(Vector3 currentPos, Vector3 targetPos)
        {
            var enemyDirection = targetPos - currentPos;
            var distance = enemyDirection.magnitude;
            enemyDirection.Normalize();
            var movementDirectionDueToGun = GetMoveDirectionDueToGun(distance, enemyDirection).normalized;

            var path2 = navSystem.CalculatePath(
                currentPos + movementDirectionDueToGun * navSystem.Speed * UPDATE_PERIOD);
            if (path2.IsValid())
            {
                nextUpdateTime = Time.time + UPDATE_PERIOD;
                updateOnlyDueToTimeout = true;
                navSystem.SetPath(path2);
            }
        }


        private Vector3 GetMovementDirectionDueToStrife(Vector3 direction)
        {
            var movementDirectionDueToStrife = Vector3.zero;
            var enemyLookDirection = target.transform.forward;
            var lookAngleRelativeToDirection = Vector3.SignedAngle(enemyLookDirection, direction, Vector3.up);
            if (Mathf.Abs(lookAngleRelativeToDirection) <= 90) return movementDirectionDueToStrife;

            // Only strife if enemy is looking us
            movementDirectionDueToStrife = Vector3.Cross(direction, transform.up);
            remainingStrafes--;
            if (remainingStrafes < 0)
            {
                remainingStrafes = Random.Range(MIN_STRAFE_LENGTH, MAX_STRAFE_LENGTH);
                strifeRight = !strifeRight;
            }

            if (!strifeRight) movementDirectionDueToStrife = -movementDirectionDueToStrife;

            return movementDirectionDueToStrife;
        }

        private Vector3 GetMoveDirectionDueToGun(float distance, Vector3 direction)
        {
            // TODO avoid overshooting! Move just the right distance to get to the desired distance, not more than that.
            Vector3 movementDirectionDueToGun;
            // Get current gun optimal range
            var (closeRange, farRange) = gunManager.GetCurrentGunOptimalRange();
            var rangeSize = farRange - closeRange;

            var skillAdjustedCloseRange = closeRange + rangeSize * entity.GunMovementCorrectness / 100; 
            var skillAdjustedFarRange = farRange + rangeSize * entity.GunMovementCorrectness / 100;

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
                // TODO Perhaps have a flag like strife to avoid oscillating too much
                movementDirectionDueToGun = direction * (Random.value > 0.5f ? -0.01f : 0.01f);
            return movementDirectionDueToGun;
        }
    }
}