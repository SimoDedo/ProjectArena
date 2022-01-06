using System;
using Accord.Statistics.Distributions.Univariate;
using AssemblyAI.AI.Layer1.Actuator;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyAI.AI.Layer2;
using AssemblyEntity.Component;
using UnityEngine;

namespace AssemblyAI.Actions
{
    public class ActionAimGun
    {
        private const int MAX_LOOK_AHEAD_FRAMES = 5;
        private const float LOOK_AHEAD_TIME_STEP = 0.1f;
        private readonly Transform transform;
        private readonly GunManager gunManager;
        private readonly Entity enemy;
        private readonly PositionTracker enemyPositionTracker;
        private readonly AISightController sightController;
        private readonly AISightSensor sightSensor;
        private readonly NavigationSystem navSystem;

        private readonly NormalDistribution distribution;
        private float previousReflexDelay;
        private float targetReflexDelay;
        private float nextDelayRecalculation = float.MinValue;
        private const float UPDATE_INTERVAL = 0.5f;

        private int previousGunIndex;

        public ActionAimGun(AIEntity entity)
        {
            transform = entity.transform;
            gunManager = entity.GunManager;
            sightController = entity.SightController;
            sightSensor = entity.SightSensor;
            navSystem = entity.NavigationSystem;
            enemy = entity.GetEnemy();
            enemyPositionTracker = enemy.GetComponent<PositionTracker>();

            var skill = entity.GetAimingSkill();

            // TODO Find better values
            var mean = 0.4f - 0.6f * skill; // [-0.2, 0.4]
            var stdDev = 0.3f - 0.1f * skill; // [0.2, 0.3]

            distribution = new NormalDistribution(mean, stdDev);
            targetReflexDelay = (float) distribution.Generate();

            previousGunIndex = gunManager.CurrentGunIndex;
        }

        public void Perform()
        {
            if (previousGunIndex != gunManager.CurrentGunIndex || nextDelayRecalculation <= Time.time)
            {
                previousReflexDelay = targetReflexDelay;
                targetReflexDelay = (float) distribution.Generate();
                nextDelayRecalculation = Time.time + UPDATE_INTERVAL;
            }

            previousGunIndex = gunManager.CurrentGunIndex;

            var currentDelay =
                previousReflexDelay + (targetReflexDelay - previousReflexDelay) *
                (Time.time - (nextDelayRecalculation - UPDATE_INTERVAL)) / UPDATE_INTERVAL;

            var (position, velocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(currentDelay);

            float angle;
            var projectileSpeed = gunManager.GetCurrentGunProjectileSpeed();
            if (float.IsPositiveInfinity(projectileSpeed))
            {
                angle = sightController.LookAtPoint(position);
                if (angle < 10 && gunManager.CanCurrentGunShoot())
                {
                    if (!Physics.Linecast(sightController.GetHeadPosition(), position, out var hitInfo) ||
                        hitInfo.collider.gameObject == enemy.gameObject)
                    {
                        // I can directly see that point or I found an obstacle, but it's the enemy itself, so shoot!
                        gunManager.ShootCurrentGun();
                    }
                }
            }
            else
            {
                // var distance = (transform.position - enemy.transform.position).magnitude;
                // var estimatedTime = distance / projectileSpeed;

                var ourStartingPoint = transform.position;
                var enemyStartPos = enemy.transform.position;
                // Default value: point on ground underneath enemy
                var record = float.PositiveInfinity;

                var chosenPoint = Vector3.zero;
                for (var i = 0; i <= MAX_LOOK_AHEAD_FRAMES; i++)
                {
                    var newPos = enemyStartPos + velocity * (i * LOOK_AHEAD_TIME_STEP);
                    // TODO NavMeshCheck
                    if (navSystem.IsPointOnNavMesh(newPos, out var hit))
                    {
                        newPos = hit;
                        if (!sightSensor.CanSeePosition(newPos)) continue;
                        var distance = (ourStartingPoint - newPos).magnitude;
                        var rocketTravelDistance = i * LOOK_AHEAD_TIME_STEP * projectileSpeed;
                        // if (rocketTravelDistance > distance) // Perfect, found our solution!
                        // {
                        //     chosenPoint = newPos;
                        //     break;
                        // }
                        if (distance - rocketTravelDistance < record)
                        {
                            record = distance - rocketTravelDistance;
                            chosenPoint = newPos;
                        }
                    }
                }

                // Only shoot if we found a nice position, otherwise keep the shot for another time
                if (float.IsPositiveInfinity(record)) return;
                angle = sightController.LookAtPoint(chosenPoint);
                if (angle < 40 && gunManager.CanCurrentGunShoot()) gunManager.ShootCurrentGun();
            }
        }
    }
}