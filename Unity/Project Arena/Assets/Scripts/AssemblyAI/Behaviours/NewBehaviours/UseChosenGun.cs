using System;
using Accord.Statistics.Distributions.Univariate;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using Entities.AI.Layer2;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.NewBehaviours.Variables
{
    [Serializable]
    public class UseChosenGun : Action
    {
        [SerializeField] private SharedInt chosenGunIndex;
        [SerializeField] private int maxLookAheadFrames = 5;
        [SerializeField] private float lookAheadTimeStep = 0.1f;
        private AIEntity entity;
        private Entity enemy;
        private PositionTracker enemyPositionTracker;
        private AISightController sightController;
        private AISightSensor sightSensor;
        private NavigationSystem navSystem;
        private Gun gun;

        private NormalDistribution distribution;
        private float reflexDelay;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
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
        }

        public override void OnStart()
        {
            // TODO check this is called at the right time
            entity.EquipGun(chosenGunIndex.Value);
            gun = entity.GetCurrentGun();
            reflexDelay = (float) distribution.Generate();
        }

        public override TaskStatus OnUpdate()
        {
            var (position, velocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(reflexDelay);

            if (!gun.CanShoot())
            {
                sightController.LookAtPoint(position);
                return TaskStatus.Running;
            }

            float angle;
            var projectileSpeed = gun.GetProjectileSpeed();
            if (float.IsPositiveInfinity(projectileSpeed))
            {
                angle = sightController.LookAtPoint(position);
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
                        var rocketTravelDistance = (i * lookAheadTimeStep) * projectileSpeed;
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
            }

            if (angle < 40)
                gun.Shoot();


            return TaskStatus.Running;
        }
    }
}