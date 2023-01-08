using System;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Guns;
using UnityEngine;

// TODO

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Detects whether there is a rocket to dodge and, saving it and returning Success in case there is or returning
    /// Failure otherwise.
    /// </summary>
    [Serializable]
    public class HasRocketToDodge : Conditional
    {
        private const float ROCKET_DETECTION_RADIUS = 40f;

        private static readonly Collider[] RocketTestCollider = new Collider[4];
        [SerializeField] private SharedTransform rocketToDodge;
        private int layerMask;
        private int myID;
        private SightSensor sightSensor;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            myID = entity.GetID();
            // movementSkill = entity.MovementSkill;
            sightSensor = entity.SightSensor;

            layerMask = LayerMask.GetMask("Default", "Projectile", "Wall");
        }

        public override TaskStatus OnUpdate()
        {
            // if (movementSkill < FightingMovementSkill.CircleStrife)
                // Not skilled enough to dodge
                return TaskStatus.Failure;

            // var projectileToDodge = FindRocketToDodge();
            // if (projectileToDodge == null) return TaskStatus.Failure;
            //
            // // TODO should add delay to rocket detection?
            // rocketToDodge.Value = projectileToDodge.transform;
            // return TaskStatus.Success;
        }

        // Finds if there is any rocket in view which is not mine and needs to be dodged.
        private Projectile FindRocketToDodge()
        {
            Projectile projectileToDodge = null;

            // Detect rocket presence
            var position = transform.position;
            var projectileColliders = Physics.OverlapSphereNonAlloc(
                position,
                ROCKET_DETECTION_RADIUS,
                RocketTestCollider,
                layerMask
            );

            var closestRocket = float.MaxValue;

            if (projectileColliders == 0)
                // No projectile around me
                return null;

            // Check if there is a projectile which projectile doesn't belong to me
            for (var i = 0; i < projectileColliders; i++)
            {
                var projectile = RocketTestCollider[i].GetComponent<Projectile>();
                if (projectile == null)
                    // TODO When does this happen?
                    continue;

                if (projectile.ShooterID == myID)
                    // You should not worry about your own rockets
                    continue;

                // This rocket is not mine... How do I dodge it?
                // Calculate it's trajectory and try to get away from it

                var projectileTransform = projectile.transform;

                if (!sightSensor.CanSeeObject(projectileTransform))
                    // Cannot react to rockets I do not see!
                    continue;

                var projectilePosition = projectileTransform.position;
                var distance = (position - projectilePosition).sqrMagnitude;
                if (distance < closestRocket)
                {
                    // calculate angle, ignore projectiles that are moving  
                    var projectileDirection = projectileTransform.forward;
                    var myDirection = position - projectilePosition;

                    if (Vector3.Angle(myDirection, projectileDirection) > 45)
                        // Rocket is getting away from us, no need to dodge
                        continue;

                    closestRocket = distance;
                    projectileToDodge = projectile;
                }
            }

            return projectileToDodge;
        }
    }
}