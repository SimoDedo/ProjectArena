using System;
using Accord.Statistics.Distributions.Univariate;
using AI.KnowledgeBase;
using AssemblyAI.AI.Layer1.Actuator;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class UseChosenGun : Action
    {
        [SerializeField] private int maxLookAheadFrames = 5;
        [SerializeField] private float lookAheadTimeStep = 0.1f;
        [SerializeField] private SharedBool isGoingForCover;
        private AIEntity entity;
        private GunManager gunManager;
        private Entity enemy;
        private PositionTracker enemyPositionTracker;
        private TargetKnowledgeBase targetKb;
        private AISightController sightController;
        private AISightSensor sightSensor;

        private NormalDistribution distribution;
        private float previousReflexDelay;
        private float targetReflexDelay;
        private float nextDelayRecalculation = float.MinValue;
        private const float UPDATE_INTERVAL = 0.5f;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            gunManager = entity.GunManager;
            targetKb = entity.TargetKb;
            sightController = entity.SightController;
            sightSensor = entity.SightSensor;
            enemy = entity.GetEnemy();
            enemyPositionTracker = enemy.GetComponent<PositionTracker>();

            // TODO Find better values
            var skill = entity.GetAimingSkill();
            var mean = 0.4f - 0.6f * skill; // [-0.2, 0.4]
            var stdDev = 0.3f - 0.1f * skill; // [0.2, 0.3]
            distribution = new NormalDistribution(mean, stdDev);
            targetReflexDelay = (float) distribution.Generate();
        }

        public override TaskStatus OnUpdate()
        {
            var lastSightedTime = targetKb.GetLastSightedTime();
            if (lastSightedTime != Time.time && isGoingForCover.Value && !gunManager.IsCurrentGunReloading())
            {
                //We cannot see the enemy and we were looking for cover, reload now!
                gunManager.ReloadCurrentGun();
            }

            if (nextDelayRecalculation <= Time.time)
            {
                previousReflexDelay = targetReflexDelay;
                targetReflexDelay = (float) distribution.Generate();
                nextDelayRecalculation = Time.time + UPDATE_INTERVAL;
            }

            var currentDelay =
                previousReflexDelay + (targetReflexDelay - previousReflexDelay) *
                (Time.time - (nextDelayRecalculation - UPDATE_INTERVAL)) / UPDATE_INTERVAL;

            var lastSightedDelay = Time.time - lastSightedTime;

            Vector3 position;
            Vector3 velocity;

            if (!float.IsNaN(lastSightedDelay) && lastSightedDelay > currentDelay)
            {
                // We don't know the exact position of the enemy currentDelay ago, so just consider
                // its last know position (with velocity zero, so that we won't correct the shooting position)
                (position, velocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(lastSightedDelay);
            }
            else
            {
                // We know the position of the enemy at currentDelay seconds ago, so use it directly.
                (position, velocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(currentDelay);
            }

            float angle;
            var projectileSpeed = gunManager.GetCurrentGunProjectileSpeed();
            if (float.IsPositiveInfinity(projectileSpeed))
            {
                // TODO Do not shoot if outside of gun range
                angle = sightController.LookAtPoint(position);
                if (lastSightedTime != Time.time && !gunManager.IsGunBlastWeapon(gunManager.CurrentGunIndex))
                {
                    // We don't see the enemy and we are not using a blast weapon, do not shoot.
                    return TaskStatus.Running;
                }
                if (angle < 10 && gunManager.CanCurrentGunShoot() &&
                    ShouldShootWeapon(sightController.GetHeadPosition(), position))
                {
                    gunManager.ShootCurrentGun();
                }
            }
            else
            {
                // var distance = (transform.position - enemy.transform.position).magnitude;
                // var estimatedTime = distance / projectileSpeed;

                var ourStartingPoint = sightController.GetHeadPosition();
                var enemyStartPos = position;
                // Default value: point on ground underneath enemy
                var record = float.PositiveInfinity;
                var chosenPoint = Vector3.zero;
                for (var i = 0; i <= maxLookAheadFrames; i++)
                {
                    var newPos = enemyStartPos + velocity * ((i-2) * lookAheadTimeStep);

                    if (gunManager.IsGunBlastWeapon(gunManager.CurrentGunIndex))
                    {
                        // The weapon is a blast weapon. Aim at the enemy's feet.
                        // TODO Check this raycast works
                        Physics.Raycast(newPos, Vector3.down, out var hit);
                        newPos = hit.point;
                    }

                    if (!sightSensor.CanSeePosition(newPos)) continue;
                    var distance = (ourStartingPoint - newPos).magnitude;
                    var projectileTravelDistance = i * lookAheadTimeStep * projectileSpeed;
                    if (distance - projectileTravelDistance < record)
                    {
                        record = distance - projectileTravelDistance;
                        chosenPoint = newPos;
                    }
                }

                // Only shoot if we found a nice position, otherwise keep the shot for another time
                if (float.IsPositiveInfinity(record)) return TaskStatus.Running;
                
                angle = sightController.LookAtPoint(chosenPoint);
                if (angle < 40 && gunManager.CanCurrentGunShoot() &&
                    ShouldShootWeapon(sightController.GetHeadPosition(), chosenPoint))
                {
                    gunManager.ShootCurrentGun();
                }
            }


            return TaskStatus.Running;
        }

        // I can shoot a gun if I do not detect any obstacle in the path from startingPos to position.
        // An obstacle is defined as something that would prevent the bullet from reaching at least 0.8
        // times the distance from startingPos to position.
        private bool ShouldShootWeapon(Vector3 startingPos, Vector3 position)
        {
            var distance = (startingPos - position).magnitude;
            if (gunManager.GetGunMaxRange(gunManager.CurrentGunIndex) < distance)
            {
                // Outside of weapon range...
                return false;
            }
            // Try to cast a ray from our position to position. If it's too short, we have an obstacle
            // in front of us...
            var canShoot = false;
            var raycastResult = Physics.Linecast(startingPos, position, out var hit);
            if (raycastResult == false)
            {
                canShoot = true;
            }
            else
            {
                var distanceRatio = hit.distance / distance;
                if (distanceRatio > 0.8f)
                {
                    canShoot = true;
                }
            }

            return canShoot;
        }
    }
}