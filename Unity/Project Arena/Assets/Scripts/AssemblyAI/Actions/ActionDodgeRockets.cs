using UnityEngine;

namespace AI.Actions
{
    public static class ActionDodgeRockets
    {
        private static readonly Collider[] RocketTestCollider = new Collider[4];
        private const float ROCKET_DETECTION_RADIUS = 40f;

        public static void Perform(AIEntity entity)
        {
            var skill = entity.MovementSkill;

            if (skill >= FightingMovementSkill.CircleStrife)
            {
                var rocketToDodge = FindRocketToDodge(entity);
                if (rocketToDodge != null)
                {
                    CalculateDodgePosition(entity, rocketToDodge);
                }
            }
        }

        private static Projectile FindRocketToDodge(AIEntity entity)
        {
            var skill = entity.MovementSkill;

            Projectile projectileToDodge = null;
            if (skill < FightingMovementSkill.CircleStrife)
            {
                // Too dumb to avoid rockets
                return null;
            }
            var sightSensor = entity.SightSensor;
            var transform = entity.transform;

            // Detect rocket presence
            var position = transform.position;
            var projectileColliders = Physics.OverlapSphereNonAlloc(
                position,
                ROCKET_DETECTION_RADIUS,
                RocketTestCollider,
                1 << LayerMask.NameToLayer("Projectile")
            );

            var closestRocket = float.MaxValue;

            if (projectileColliders != 0)
            {
                // Check if any projectile doesn't belong to me
                for (var i = 0; i < projectileColliders; i++)
                {
                    var projectile = RocketTestCollider[i].GetComponent<Projectile>();
                    if (projectile == null) continue;
                    if (projectile.ShooterID != entity.GetID())
                    {
                        // This rocket is not mine... How do I dodge it?
                        // Calculate it's trajectory and try to get away from it

                        var projectileTransform = projectile.transform;
                        var projectilePosition = projectileTransform.position;
                        var distance = (position - projectilePosition).sqrMagnitude;

                        if (!sightSensor.CanSeeObject(projectileTransform, Physics.DefaultRaycastLayers))
                        {
                            // Cannot react to rockets I do not see!
                            continue;
                        }
                        
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

            return projectileToDodge;
        }

        private static void CalculateDodgePosition(AIEntity entity, Projectile rocketToDodge)
        {
            var transform = entity.transform;
            var navSystem = entity.NavigationSystem;

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
            if (angle > 0f) avoidDirection = -avoidDirection;
            Debug.DrawLine(
                transform.position,
                transform.position + avoidDirection * navSystem.Speed,
                Color.cyan,
                0f,
                true
            );
            navSystem.SetDestination(transform.position + avoidDirection);
        }
    }
}