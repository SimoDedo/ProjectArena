using Logging;
using UnityEngine;

namespace AI.Layers.SensingLayer
{
    public class SoundSensor
    {
        private readonly float recentNoiseTimeout;
        private readonly float recentNoiseDelay;
        private readonly int entityID;
        private readonly float soundThreshold;
        private readonly Transform t;
        private const int MAX_BACKLOG_SHOTS = 16;
        private readonly float[] lastShotsHeard = new float[MAX_BACKLOG_SHOTS];
        private int latestShotIndex;
        
        public SoundSensor(float recentNoiseDelay, float recentNoiseTimeout, int entityID, Transform transform, float soundThreshold)
        {
            Reset();
            this.recentNoiseDelay = recentNoiseDelay;
            this.recentNoiseTimeout = recentNoiseTimeout;
            this.entityID = entityID; 
            this.soundThreshold = soundThreshold;
            t = transform;
            ShootingSoundGameEvent.Instance.AddListener(ListenForGun);
        }
        
        /// <summary>
        /// Get the last time the entity heard another one's gun.
        /// </summary>
        public float LastTimeHeardShot => lastShotsHeard[latestShotIndex];

        /// <summary>
        /// Returns whether the entity recently heard a suspicious noise.
        /// </summary>
        public bool HeardShotRecently
        {
            get
            {
                var foundInterval = false;
                for (var i = 0; i < MAX_BACKLOG_SHOTS && !foundInterval; i++)
                {
                    var timeDiff = Time.time - lastShotsHeard[i] - recentNoiseDelay;
                    foundInterval = timeDiff >= 0 && timeDiff < recentNoiseTimeout;
                }

                return foundInterval;
            }
        }

        /// <summary>
        /// Forgets the time the entity last heard a gun shot.
        /// </summary>
        public void Reset()
        {
            latestShotIndex = 0;
            for (var i = 0; i < MAX_BACKLOG_SHOTS; i++)
            {
                lastShotsHeard[i] = float.MinValue;
            }
        }

        private void ListenForGun(GunShootingSoundInfo info)
        {
            if (info.gunOwnerId == entityID)
            {
                // Ignore since it's our own gun
                return;
            }
            
            var sqrDistance = (t.position - info.gunPosition).sqrMagnitude;
            var soundIntensity = info.gunLoudness / sqrDistance;

            if (soundIntensity <= soundThreshold) return;

            latestShotIndex = (latestShotIndex + 1) % MAX_BACKLOG_SHOTS;
            lastShotsHeard[latestShotIndex] = Time.time;
        }

        public void Release()
        {
            ShootingSoundGameEvent.Instance.RemoveListener(ListenForGun);
        }
    }
}