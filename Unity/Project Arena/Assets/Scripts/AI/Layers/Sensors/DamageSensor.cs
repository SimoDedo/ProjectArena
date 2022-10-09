using UnityEngine;

namespace AI.Layers.SensingLayer
{
    /// <summary>
    /// This AI component is able to keep track on when the entity has been damaged and whether or not the entity
    /// was recently damaged.
    /// </summary>
    ///  TODO Extract detection of damage and keeping track of whether the entity should react to it or not.
    public class DamageSensor
    {
        /// <summary>
        /// Timeout after taking damage for considering the entity as "recently damaged".
        /// </summary>
        private readonly float recentTimeout;

        /// <summary>
        /// Delay before registering that damage was taken.
        /// </summary>
        private readonly float reactionDelay;

        public DamageSensor(float recentTimeout, float reactionDelay)
        {
            this.recentTimeout = recentTimeout;
            this.reactionDelay = reactionDelay;
        }

        /// <summary>
        /// Get the last time the entity was damaged.
        /// </summary>
        public float LastTimeDamaged { get; private set; } = float.MinValue;

        /// <summary>
        /// Returns whether the entity was "recently damaged" and can react to the event.
        /// </summary>
        public bool WasDamagedRecently
        {
            get
            {
                var timeDiff = Time.time - LastTimeDamaged;
                return timeDiff >= reactionDelay && timeDiff < recentTimeout;
            }
        }

        /// <summary>
        /// To be called when the entity got damage, so as to update the LastTimeDamaged.
        /// </summary>
        public void GotDamaged()
        {
            LastTimeDamaged = Time.time;
        }

        /// <summary>
        /// Forgets the time the entity was last damaged.
        /// </summary>
        public void Reset()
        {
            LastTimeDamaged = float.MinValue;
        }
    }
}