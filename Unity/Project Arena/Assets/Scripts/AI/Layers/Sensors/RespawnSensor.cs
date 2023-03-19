using Logging;
using UnityEngine;

namespace AI.Layers.Sensors
{
    /// <summary>
    /// This AI component is able to detect when an enemy respawned, with a probability dependent by the skill.
    /// </summary>
    ///  TODO clear listener when this object is no longer in use.
    public class RespawnSensor
    {
        /// <summary>
        /// Timeout after which the entity forgets about the respawn.
        /// </summary>
        private readonly float recentTimeout;

        /// <summary>
        /// Probability for the entity to understand where the enemy respawned.
        /// </summary>
        private readonly float detectionProbability;

        private readonly int myId;
        
        private const int MAX_BACKLOG_RESPAWNS = 16;
        private readonly float[] lastRespawnTimes = new float[MAX_BACKLOG_RESPAWNS];
        private int latestRespawnTimeIndex;

        
        public RespawnSensor(float recentTimeout, float detectionProbability, int entityId)
        {
            Reset();
            this.recentTimeout = recentTimeout;
            this.detectionProbability = detectionProbability;
            this.myId = entityId;
            SpawnInfoGameEvent.Instance.AddListener(DetectRespawn);
            
        }
        
        /// <summary>
        /// Get the last time the entity was damaged.
        /// </summary>
        public float LastRespawnTime => lastRespawnTimes[latestRespawnTimeIndex];

        /// <summary>
        /// Returns whether the entity was "recently damaged" and can react to the event.
        /// </summary>
        public bool DetectedRespawnRecently
        {
            get
            {
                var foundInterval = false;
                for (var i = 0; i < MAX_BACKLOG_RESPAWNS && !foundInterval; i++)
                {
                    var timeDiff = Time.time - lastRespawnTimes[i];
                    foundInterval = timeDiff >= 0 && timeDiff < recentTimeout;
                }

                return foundInterval;
            }
        }

        /// <summary>
        /// To be called when the entity respawns, so as to update the LastTimeDamaged.
        /// </summary>
        private void DetectRespawn(SpawnInfo obj)
        {
            if (obj.entityId == myId) return;
            if (Random.value > detectionProbability)
            {
                return;
            }
            latestRespawnTimeIndex = (latestRespawnTimeIndex + 1) % MAX_BACKLOG_RESPAWNS;
            lastRespawnTimes[latestRespawnTimeIndex] = Time.time;
        }


        /// <summary>
        /// Forgets the respawn events.
        /// </summary>
        public void Reset()
        {
            latestRespawnTimeIndex = 0;
            for (var i = 0; i < MAX_BACKLOG_RESPAWNS; i++)
            {
                lastRespawnTimes[i] = float.MinValue;
            }
        }
    }
}
