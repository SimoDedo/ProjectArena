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
        /// Timeout after which the entity forgets about having took damage.
        /// </summary>
        private readonly float recentTimeout;

        /// <summary>
        /// Delay before registering that damage was taken.
        /// </summary>
        private readonly float reactionDelay;

        private const int MAX_BACKLOG_DAMAGES = 16;
        private readonly float[] lastDamagedTimes = new float[MAX_BACKLOG_DAMAGES];
        private int latestDamageTimeIndex;

        
        public DamageSensor(float reactionDelay, float recentTimeout)
        {
            Reset();
            this.recentTimeout = recentTimeout;
            this.reactionDelay = reactionDelay;
        }

        /// <summary>
        /// Get the last time the entity was damaged.
        /// </summary>
        public float LastTimeDamaged => lastDamagedTimes[latestDamageTimeIndex];

        /// <summary>
        /// Returns whether the entity was "recently damaged" and can react to the event.
        /// </summary>
        public bool WasDamagedRecently
        {
            get
            {
                var foundInterval = false;
                for (var i = 0; i < MAX_BACKLOG_DAMAGES && !foundInterval; i++)
                {
                    var timeDiff = Time.time - lastDamagedTimes[i] - reactionDelay;
                    foundInterval = timeDiff >= 0 && timeDiff < recentTimeout;
                }

                return foundInterval;
            }
        }

        /// <summary>
        /// To be called when the entity got damage, so as to update the LastTimeDamaged.
        /// </summary>
        public void GotDamaged()
        {
            latestDamageTimeIndex = (latestDamageTimeIndex + 1) % MAX_BACKLOG_DAMAGES;
            lastDamagedTimes[latestDamageTimeIndex] = Time.time;
        }

        /// <summary>
        /// Forgets the time the entity was last damaged.
        /// </summary>
        public void Reset()
        {
            latestDamageTimeIndex = 0;
            for (var i = 0; i < MAX_BACKLOG_DAMAGES; i++)
            {
                lastDamagedTimes[i] = float.MinValue;
            }

        }
    }
}
