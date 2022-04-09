using Logging;
using UnityEngine;

namespace AI.Layers.SensingLayer
{
    public class SoundSensor
    {
        private readonly float recentNoiseTimeout;
        private readonly int entityID;
        private readonly float soundThreshold;
        private readonly Transform t;

        public SoundSensor(float recentNoiseTimeout, int entityID, Transform transform, float soundThreshold)
        {
            this.recentNoiseTimeout = recentNoiseTimeout;
            this.entityID = entityID; 
            this.soundThreshold = soundThreshold;
            t = transform;
            ShootingSoundGameEvent.Instance.AddListener(ListenForGun);
        }
        
        /// <summary>
        /// Get the last time the entity heard another one's gun.
        /// </summary>
        public float LastTimeHeardShot { get; private set; } = float.MinValue;

        /// <summary>
        /// Returns whether the entity was "recently damaged".
        /// </summary>
        public bool HeardShotRecently => LastTimeHeardShot + recentNoiseTimeout >= Time.time;
        

        /// <summary>
        /// Forgets the time the entity last heard a gun shot.
        /// </summary>
        public void Reset()
        {
            LastTimeHeardShot = float.MinValue;
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

            if (soundIntensity > soundThreshold)
            {
                LastTimeHeardShot = Time.time;
            }
        }

        public void Release()
        {
            ShootingSoundGameEvent.Instance.RemoveListener(ListenForGun);
        }
    }
}