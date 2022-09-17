using System;
using AI.Layers.Actuators;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entity;
using Entity.Component;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;
using Random = UnityEngine.Random;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Fight using the gun currently equipped.
    /// </summary>
    [Serializable]
    public class UseEquippedGun : Action
    {
        // Time (in seconds) after which recalculating the aim delay.
        private const float AIM_UPDATE_INTERVAL = 0.5f;
        [SerializeField] private int lookBackFrames = -3;
        [SerializeField] private int lookAheadFrames = 3;
        [SerializeField] private float lookAheadTimeStep = 0.3f;
        [SerializeField] private SharedBool isGoingForCover;

        private Entity.Entity enemy;
        private PositionTracker enemyPositionTracker;
        private AIEntity entity;
        private GunManager gunManager;
        private float nextDelayRecalculation = float.MinValue;
        private float previousReflexDelay;
        private SightController sightController;
        private TargetKnowledgeBase _targetKnowledgeBase;
        private float targetReflexDelay;
        private NormalDistribution distribution;
        
        public override void OnAwake()
        {
            entity = GetComponent<AIEntity>();
            gunManager = entity.GunManager;
            _targetKnowledgeBase = entity.TargetKnowledgeBase;
            sightController = entity.SightController;
            enemy = entity.GetEnemy();
            enemyPositionTracker = enemy.GetComponent<PositionTracker>();

            // TODO Find better values
            var skill = entity.AimingSkill;
            const float stdDev = 0.03f;
            var mean = 0.100f - skill * 0.150f;

            distribution = new NormalDistribution(mean, stdDev);
            targetReflexDelay = (float) distribution.Generate();
        }

        public override void OnEnd()
        {
            if (gunManager.IsCurrentGunAiming())
            {
                gunManager.SetCurrentGunAim(false);
            }
        }

        public override TaskStatus OnUpdate()
        {
            var lastSightedTime = _targetKnowledgeBase.LastTimeDetected;
            if (lastSightedTime != Time.time && isGoingForCover.Value && !gunManager.IsCurrentGunReloading())
                //We cannot see the enemy and we were looking for cover, reload now!
                gunManager.ReloadCurrentGun();
            if (gunManager.GetAmmoInChargerForGun(gunManager.CurrentGunIndex) == 0 &&
                !gunManager.IsCurrentGunReloading())
            {
                // Out of ammo! No matter searching for cover or anything, start reloading now!
                gunManager.ReloadCurrentGun();
            }

            if (nextDelayRecalculation <= Time.time)
            {
                previousReflexDelay = targetReflexDelay;
                targetReflexDelay = (float) distribution.Generate();
                nextDelayRecalculation = Time.time + AIM_UPDATE_INTERVAL;
            }

            var currentDelay = Math.Max(
                0, 
                previousReflexDelay + (targetReflexDelay - previousReflexDelay) *
                    (Time.time - (nextDelayRecalculation - AIM_UPDATE_INTERVAL)) / AIM_UPDATE_INTERVAL
                );

            var lastSightedDelay = Time.time - lastSightedTime;

            Vector3 enemyPosition;
            Vector3 enemyVelocity = Vector3.zero;

            if (!float.IsNaN(lastSightedDelay) && lastSightedDelay > currentDelay)
                // We don't know the exact position of the enemy currentDelay ago, so just consider
                // its last know position (with velocity zero, so that we won't correct the shooting position)
                (enemyPosition, _) = enemyPositionTracker.GetPositionAndVelocityForRange(lastSightedTime, lastSightedTime);
            else
            {
                var additionalDelay = Mathf.Max(0, 0.080f - 0.040f * entity.AimingSkill);
                var currentStartTime = _targetKnowledgeBase.FirstTimeDetectedInEvent;
                
                var endTime = Time.time - currentDelay - additionalDelay;
                if (endTime < currentStartTime)
                {
                    return TaskStatus.Running;
                }
                // We know the position of the enemy at currentDelay seconds ago, so use it directly.
                (enemyPosition, enemyVelocity) = enemyPositionTracker.GetPositionAndVelocityForRange(currentStartTime, endTime);

                enemyPosition += enemyVelocity * additionalDelay;
            }

            if (gunManager.IsCurrentGunProjectileWeapon())
                AimProjectileWeapon(enemyPosition, enemyVelocity);
            else
                AimRaycastWeapon(enemyPosition, lastSightedTime);

            return TaskStatus.Running;
        }


        // Tries to find the best position to aim at accounting for the weapon speed and the enemy estimated velocity.
        // In case the weapon is a blast weapon, aims at the floor.
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
            TryAimIfRecommended((ourStartingPoint - chosenPoint).magnitude, angle);
            if (!(angle < 0.5) || !gunManager.CanCurrentGunShoot() || !ShouldShootWeapon(chosenPoint, isGunBlast))
                return;
            Debug.DrawRay(ourStartingPoint, sightController.GetHeadForward() * 100f, Color.blue, 2f);
            gunManager.ShootCurrentGun();
        }


        // Tries to find the best position to aim given that the weapon hits immediately.
        // In case the weapon is a blast weapon, aims at the floor.
        private void AimRaycastWeapon(Vector3 enemyPosition, float lastSightedTime)
        {
            // TODO Do not shoot if outside of gun range
            var angle = sightController.LookAtPoint(enemyPosition);
            if (lastSightedTime != Time.time && !gunManager.IsGunBlastWeapon(gunManager.CurrentGunIndex))
            {
                // We don't see the enemy and we are not using a blast weapon, do not shoot.
                return;
            }

            var currentPosition = sightController.GetHeadPosition();
            TryAimIfRecommended((currentPosition - enemyPosition).magnitude, angle);

            if (!(angle < 0.5) || !gunManager.CanCurrentGunShoot() ||
                !ShouldShootWeapon(enemyPosition, gunManager.IsCurrentGunBlastWeapon())) return;
            gunManager.ShootCurrentGun();
        }

        private void TryAimIfRecommended(float enemyDistance, float angle)
        {
            if (gunManager.CanCurrentGunAim() && angle < 10 && enemyDistance > 10)
            {
                if (!gunManager.IsCurrentGunAiming())
                    gunManager.SetCurrentGunAim(true);
            }
            else if (gunManager.IsCurrentGunAiming())
            {
                gunManager.SetCurrentGunAim(false);
            }
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

    internal class UniformDistribution
    {
        private readonly float mean;
        private readonly float halfRange;

        public UniformDistribution(float mean, float halfRange)
        {
            this.mean = mean;
            this.halfRange = halfRange;
        }

        public double Generate()
        {
            return mean + (Random.value * 2f - 1f) * halfRange;
        }
    }

    internal class NormalDistribution
    {
        private readonly float mean;
        private readonly float stdDev;

        public NormalDistribution(float mean, float stdDev)
        {
            this.mean = mean;
            this.stdDev = stdDev;
        }

        public double Generate()
        {
            return QNorm(Random.value, mean, stdDev, true, false);
        }

        /// <summary>
        /// Quantile function (Inverse CDF) for the normal distribution.
        /// </summary>
        /// <param name="p">Probability.</param>
        /// <param name="mu">Mean of normal distribution.</param>
        /// <param name="sigma">Standard deviation of normal distribution.</param>
        /// <param name="lower_tail">If true, probability is P[X <= x], otherwise P[X > x].</param>
        /// <param name="log_p">If true, probabilities are given as log(p).</param>
        /// <returns>P[X <= x] where x ~ N(mu,sigma^2)</returns>
        /// <remarks>See https://svn.r-project.org/R/trunk/src/nmath/qnorm.c</remarks>
        private static double QNorm(double p, double mu, double sigma, bool lower_tail, bool log_p)
        {
            if (double.IsNaN(p) || double.IsNaN(mu) || double.IsNaN(sigma)) return (p + mu + sigma);
            double ans;
            bool isBoundaryCase = R_Q_P01_boundaries(p, double.NegativeInfinity, double.PositiveInfinity, lower_tail,
                log_p, out ans);
            if (isBoundaryCase) return (ans);
            if (sigma < 0) return (double.NaN);
            if (sigma == 0) return (mu);

            double p_ = R_DT_qIv(p, lower_tail, log_p);
            double q = p_ - 0.5;
            double r, val;

            if (Math.Abs(q) <= 0.425) // 0.075 <= p <= 0.925
            {
                r = .180625 - q * q;
                val = q * (((((((r * 2509.0809287301226727 +
                                 33430.575583588128105) * r + 67265.770927008700853) * r +
                               45921.953931549871457) * r + 13731.693765509461125) * r +
                             1971.5909503065514427) * r + 133.14166789178437745) * r +
                           3.387132872796366608)
                      / (((((((r * 5226.495278852854561 +
                               28729.085735721942674) * r + 39307.89580009271061) * r +
                             21213.794301586595867) * r + 5394.1960214247511077) * r +
                           687.1870074920579083) * r + 42.313330701600911252) * r + 1.0);
            }
            else
            {
                r = q > 0 ? R_DT_CIv(p, lower_tail, log_p) : p_;
                r = Math.Sqrt(-((log_p && ((lower_tail && q <= 0) || (!lower_tail && q > 0))) ? p : Math.Log(r)));

                if (r <= 5) // <==> min(p,1-p) >= exp(-25) ~= 1.3888e-11
                {
                    r -= 1.6;
                    val = (((((((r * 7.7454501427834140764e-4 +
                                 .0227238449892691845833) * r + .24178072517745061177) *
                                  r + 1.27045825245236838258) * r +
                              3.64784832476320460504) * r + 5.7694972214606914055) *
                               r + 4.6303378461565452959) * r +
                           1.42343711074968357734)
                          / (((((((r *
                                         1.05075007164441684324e-9 + 5.475938084995344946e-4) *
                                     r + .0151986665636164571966) * r +
                                 .14810397642748007459) * r + .68976733498510000455) *
                                  r + 1.6763848301838038494) * r +
                              2.05319162663775882187) * r + 1.0);
                }
                else // very close to  0 or 1 
                {
                    r -= 5.0;
                    val = (((((((r * 2.01033439929228813265e-7 +
                                 2.71155556874348757815e-5) * r +
                                .0012426609473880784386) * r + .026532189526576123093) *
                                 r + .29656057182850489123) * r +
                             1.7848265399172913358) * r + 5.4637849111641143699) *
                              r + 6.6579046435011037772)
                          / (((((((r *
                                         2.04426310338993978564e-15 + 1.4215117583164458887e-7) *
                                     r + 1.8463183175100546818e-5) * r +
                                 7.868691311456132591e-4) * r + .0148753612908506148525)
                                  * r + .13692988092273580531) * r +
                              .59983220655588793769) * r + 1.0);
                }

                if (q < 0.0) val = -val;
            }

            return (mu + sigma * val);
        }

        private static bool R_Q_P01_boundaries(double p, double _LEFT_, double _RIGHT_, bool lower_tail, bool log_p,
            out double ans)
        {
            if (log_p)
            {
                if (p > 0.0)
                {
                    ans = double.NaN;
                    return (true);
                }

                if (p == 0.0)
                {
                    ans = lower_tail ? _RIGHT_ : _LEFT_;
                    return (true);
                }

                if (p == double.NegativeInfinity)
                {
                    ans = lower_tail ? _LEFT_ : _RIGHT_;
                    return (true);
                }
            }
            else
            {
                if (p < 0.0 || p > 1.0)
                {
                    ans = double.NaN;
                    return (true);
                }

                if (p == 0.0)
                {
                    ans = lower_tail ? _LEFT_ : _RIGHT_;
                    return (true);
                }

                if (p == 1.0)
                {
                    ans = lower_tail ? _RIGHT_ : _LEFT_;
                    return (true);
                }
            }

            ans = double.NaN;
            return (false);
        }

        private static double R_DT_qIv(double p, bool lower_tail, bool log_p)
        {
            return (log_p ? (lower_tail ? Math.Exp(p) : -ExpM1(p)) : R_D_Lval(p, lower_tail));
        }

        private static double R_DT_CIv(double p, bool lower_tail, bool log_p)
        {
            return (log_p ? (lower_tail ? -ExpM1(p) : Math.Exp(p)) : R_D_Cval(p, lower_tail));
        }

        private static double R_D_Lval(double p, bool lower_tail)
        {
            return lower_tail ? p : 0.5 - p + 0.5;
        }

        private static double R_D_Cval(double p, bool lower_tail)
        {
            return lower_tail ? 0.5 - p + 0.5 : p;
        }

        private static double ExpM1(double x)
        {
            if (Math.Abs(x) < 1e-5)
                return x + 0.5 * x * x;
            else
                return Math.Exp(x) - 1.0;
        }
    }
}