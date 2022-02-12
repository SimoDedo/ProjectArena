using System;
using Accord.Statistics.Distributions.Univariate;
using AI.AI.Layer1;
using AI.AI.Layer2;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entity;
using Entity.Component;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class UseChosenGun : Action
    {
        private const float UPDATE_INTERVAL = 0.5f;
        [SerializeField] private int lookBackFrames = -3;
        [SerializeField] private int lookAheadFrames = 3;
        [SerializeField] private float lookAheadTimeStep = 0.3f;
        [SerializeField] private SharedBool isGoingForCover;

        private NormalDistribution distribution;
        private Entity.Entity enemy;
        private PositionTracker enemyPositionTracker;
        private AIEntity entity;
        private GunManager gunManager;
        private float nextDelayRecalculation = float.MinValue;
        private float previousReflexDelay;
        private SightController sightController;
        private TargetKnowledgeBase targetKb;
        private float targetReflexDelay;

        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            gunManager = entity.GunManager;
            targetKb = entity.TargetKb;
            sightController = entity.SightController;
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
            var lastSightedTime = targetKb.LastTimeDetected;
            if (lastSightedTime != Time.time && isGoingForCover.Value && !gunManager.IsCurrentGunReloading())
                //We cannot see the enemy and we were looking for cover, reload now!
                gunManager.ReloadCurrentGun();

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

            Vector3 enemyPosition;
            Vector3 enemyVelocity;

            if (!float.IsNaN(lastSightedDelay) && lastSightedDelay > currentDelay)
                // We don't know the exact position of the enemy currentDelay ago, so just consider
                // its last know position (with velocity zero, so that we won't correct the shooting position)
                (enemyPosition, enemyVelocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(lastSightedDelay);
            else
                // We know the position of the enemy at currentDelay seconds ago, so use it directly.
                (enemyPosition, enemyVelocity) = enemyPositionTracker.GetPositionAndVelocityFromDelay(currentDelay);

            if (gunManager.IsCurrentGunProjectileWeapon())
                AimProjectileWeapon(enemyPosition, enemyVelocity);
            else
                AimRaycastWeapon(enemyPosition, lastSightedTime);

            return TaskStatus.Running;
        }

        /**
         * Tries to find the best position to aim at accounting for the weapon speed and the enemy estimated velocity.
         * In case the weapon is a blast weapon, positions at the
         */
        private void AimProjectileWeapon(Vector3 position, Vector3 velocity)
        {
            var projectileSpeed = gunManager.GetCurrentGunProjectileSpeed();
            var ourStartingPoint = sightController.GetHeadPosition();
            var enemyStartPos = position;
            var record = float.PositiveInfinity;
            var chosenPoint = Vector3.zero;
            var isGunBlast = gunManager.IsCurrentGunBlastWeapon();
            for (var i = lookBackFrames; i <= lookAheadFrames; i++)
            {
                var newPos = enemyStartPos + velocity * (i * lookAheadTimeStep);

                if (isGunBlast)
                    // The weapon is a blast weapon. Aim at the enemy's feet.
                    // TODO Check this raycast works. It fails sometimes... why?
                    if (Physics.Raycast(newPos, Vector3.down * 5f, out var downRayHit))
                        newPos = downRayHit.point;

                Debug.DrawLine(ourStartingPoint, newPos);
                if (Physics.Linecast(ourStartingPoint, newPos, out var hit) && hit.point != newPos)
                    // Looks like there is an obstacle from out head to that position...
                    continue;

                var timeBeforeProjectileReachesNewPos = (ourStartingPoint - newPos).magnitude / projectileSpeed;
                var timeError = Mathf.Abs(timeBeforeProjectileReachesNewPos - i * lookAheadTimeStep);

                if (timeError < record)
                {
                    record = timeError;
                    chosenPoint = newPos;
                }
            }

            // Only shoot if we found a nice position, otherwise keep the shot for another time
            if (float.IsPositiveInfinity(record)) return;

            var angle = sightController.LookAtPoint(chosenPoint);

            if (angle < 10 && gunManager.CanCurrentGunShoot() && ShouldShootWeapon(chosenPoint, isGunBlast))
            {
                Debug.DrawRay(ourStartingPoint, sightController.GetHeadForward() * 100f, Color.blue, 2f);
                gunManager.ShootCurrentGun();
            }
        }

        private void AimRaycastWeapon(Vector3 position, float lastSightedTime)
        {
            // TODO Do not shoot if outside of gun range
            var angle = sightController.LookAtPoint(position);
            if (lastSightedTime != Time.time && !gunManager.IsGunBlastWeapon(gunManager.CurrentGunIndex))
                // We don't see the enemy and we are not using a blast weapon, do not shoot.
                return;

            // TODO Understand angle... I should avoid shooting if I am using a blast weapon and the position in 
            // front of me is too close!
            if (angle < 10 && gunManager.CanCurrentGunShoot() &&
                ShouldShootWeapon(position, gunManager.IsCurrentGunBlastWeapon()))
                gunManager.ShootCurrentGun();
        }

        // I can shoot a gun if I do not detect any obstacle in the path from startingPos to position.
        // An obstacle is defined as something that would prevent the bullet from reaching at least 0.8
        // times the distance from startingPos to position.
        private bool ShouldShootWeapon(Vector3 position, bool isBlastWeapon)
        {
            var startingPos = sightController.GetHeadPosition();
            var distance = (startingPos - position).magnitude;
            var weaponRange = gunManager.GetGunMaxRange(gunManager.CurrentGunIndex);
            if (weaponRange < distance)
                // Outside of weapon range...
                return false;

            // TODO try to raycast from my position forward. If the distance I hit is not too smaller than the distance
            // of the target, than you can shoot.

            // Check that we do not hurt ourselves by shooting this weapon
            var headForward = sightController.GetHeadForward();

            var hitSomething = Physics.Raycast(startingPos, headForward, out var hit, weaponRange);
            if (hitSomething && hit.collider.gameObject != enemy.gameObject && hit.distance <= distance * 0.9f)
                // Looks like there is an obstacle between me and the point I wanted to shoot. Avoid shooting
                return false;
            if (!isBlastWeapon)
                // No more checks needed now
                return true;

            // Try to cast a ray from our position looking forward. If we hit something, check it's not too close to
            // hurt ourselves

            // TODO Avoid hardcoding radius of rocket and blast radius
            const float blastRadius = 9f; // Slightly increase for security
            var canShoot = !hitSomething || hit.distance > blastRadius;
            return canShoot;
        }
    }
}