using UnityEngine;

namespace AI.Layers.SensingLayer
{
    /// <summary>
    /// This AI component is able to keep track on when the entity has been damaged and whether or not the entity
    /// was recently damaged.
    /// </summary>
    public class DamageSensor
    {
        /// <summary>
        /// Timeout after taking damage for considering the entity as "recently damaged".
        /// </summary>
        private readonly float recentDamageTimeout;

        public DamageSensor(float recentDamageTimeout)
        {
            this.recentDamageTimeout = recentDamageTimeout;
        }

        /// <summary>
        /// Get the last time the entity was damaged.
        /// </summary>
        public float LastTimeDamaged { get; private set; } = float.MinValue;

        /// <summary>
        /// Returns whether the entity was "recently damaged".
        /// </summary>
        public bool WasDamagedRecently => LastTimeDamaged + recentDamageTimeout >= Time.time;

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