using System;
using Accord.Statistics.Distributions.Univariate;
using AssemblyEntity.Component;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime.Tasks.Unity.Math;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using Entities.AI.Layer2;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class UseChosenGun : Action
    {
        [SerializeField] private SharedInt chosenGunIndex;
        [SerializeField] private int maxLookAheadFrames = 5;
        [SerializeField] private float lookAheadTimeStep = 0.1f;
        private AIEntity entity;
        private GunManager gunManager;
        private Entity enemy;
        private PositionTracker enemyPositionTracker;
        private AISightController sightController;
        private AISightSensor sightSensor;
        private NavigationSystem navSystem;

        private NormalDistribution distribution;
        private float previousReflexDelay;
        private float targetReflexDelay;
        private float nextDelayRecalculation = float.MinValue;
        private const float UPDATE_INTERVAL = 0.5f;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            gunManager = GetComponent<GunManager>();
            sightController = GetComponent<AISightController>();
            sightSensor = GetComponent<AISightSensor>();
            navSystem = GetComponent<NavigationSystem>();
            enemy = entity.GetEnemy();
            enemyPositionTracker = enemy.GetComponent<PositionTracker>();

            // TODO Find better values
            var skill = entity.GetAimingSkill();
            var mean = 0.4f - 0.6f * skill; // [-0.2, 0.4]
            var stdDev = 0.3f - 0.1f * skill; // [0.2, 0.3]
            distribution = new NormalDistribution(mean, stdDev);
            targetReflexDelay = (float) distribution.Generate();
        }

        public override void OnStart()
        {
            // TODO What if this fails?
            gunManager.TryEquipGun(chosenGunIndex.Value);
            
        }

        public override TaskStatus OnUpdate()
        {
            if (nextDelayRecalculation <= Time.time)
            {
                previousReflexDelay = targetReflexDelay;
                targetReflexDelay = (float) distribution.Generate();
                nextDelayRecalculation = Time.time + UPDATE_INTERVAL;
            }
            
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
                    gunManager.ShootCurrentGun();
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
                for (var i = 0; i <= maxLookAheadFrames; i++)
                {
                    var newPos = enemyStartPos + velocity * (i * lookAheadTimeStep);
                    // TODO NavMeshCheck
                    if (navSystem.IsPointOnNavMesh(newPos, out var hit))
                    {
                        newPos = hit;
                        if (!sightSensor.CanSeePosition(newPos)) continue;
                        var distance = (ourStartingPoint - newPos).magnitude;
                        var rocketTravelDistance = i * lookAheadTimeStep * projectileSpeed;
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
                if (float.IsPositiveInfinity(record)) return TaskStatus.Running;
                angle = sightController.LookAtPoint(chosenPoint);
                if (angle < 40 && gunManager.CanCurrentGunShoot())
                    gunManager.ShootCurrentGun();
            }



            return TaskStatus.Running;
        }
    }
}