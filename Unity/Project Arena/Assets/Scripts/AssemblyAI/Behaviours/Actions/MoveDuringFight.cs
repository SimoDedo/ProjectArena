using System;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class MoveDuringFight : Action
    {
        private AIEntity entity;
        private NavigationSystem agent;
        private Entity target;

        private bool strifeRight = Random.value < 0.5;
        private int remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);

        private const int minStrifeLength = 10;
        private const int maxStrifeLength = 30;
        private AIEntity.FightingMovementSkill skill;
        private Collider[] rocketTestCollider = new Collider[4];
        private const float ROCKET_DETECTION_RADIUS = 40f;

        public override void OnStart()
        {
            agent = GetComponent<NavigationSystem>();
            entity = GetComponent<AIEntity>();
            target = entity.GetEnemy();
            skill = entity.GetMovementSkill();
            agent.CancelPath();
        }

        public override void OnEnd()
        {
            agent.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            // Is able enough to dodge rockets?
            if (skill >= AIEntity.FightingMovementSkill.CircleStrife)
            {
                var rocketToDodge = FindRocketToDodge();
                if (rocketToDodge != null)
                {
                    DodgeRocket(rocketToDodge);
                    return TaskStatus.Running;
                }
            }

            if (agent.HasPath() && !agent.HasArrivedToDestination())
                return TaskStatus.Running;
            TrySelectDestination();
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
            agent.SetDestination(transform.position + avoidDirection * agent.GetSpeed());
            Debug.DrawLine(transform.position, transform.position + avoidDirection * agent.GetSpeed(), Color.magenta);
        }

        private Projectile FindRocketToDodge()
        {
            Projectile projectileToDodge = null;
            if (skill >= AIEntity.FightingMovementSkill.CircleStrife)
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
            if (skill == AIEntity.FightingMovementSkill.StandStill) return;

            var currentPos = transform.position;
            var targetPos = target.transform.position;
            targetPos.y = currentPos.y;
            var unNormalizedDirection = targetPos - currentPos;
            var direction = unNormalizedDirection.normalized;

            if (skill == AIEntity.FightingMovementSkill.MoveStraight)
            {
                // TODO how to decide if moving forward or backward?
                if (unNormalizedDirection.sqrMagnitude > 25f)
                    agent.SetDestination(currentPos + direction * 3f);
                else
                    agent.SetDestination(currentPos + -direction * 3f);
                return;
            }

            var strifeDir = Vector3.Cross(direction, transform.up);
            if (skill == AIEntity.FightingMovementSkill.CircleStrifeChangeDirection)
            {
                remainingStrifes--;
                if (remainingStrifes < 0)
                {
                    remainingStrifes = Random.Range(minStrifeLength, maxStrifeLength);
                    strifeRight = !strifeRight;
                }
            }

            var offset = strifeDir * (strifeRight ? 3f : -3f);
            Debug.DrawLine(currentPos, currentPos + offset, Color.magenta);
            agent.SetDestination(currentPos + offset);
        }
    }
}