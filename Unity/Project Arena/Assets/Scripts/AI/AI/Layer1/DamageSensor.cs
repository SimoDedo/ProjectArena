using UnityEngine;

namespace AI.AI.Layer1
{
    public class DamageSensor
    {
        private readonly float recentDamageTimeout;

        public DamageSensor(float recentDamageTimeout)
        {
            this.recentDamageTimeout = recentDamageTimeout;
        }

        public float LastTimeDamaged { get; private set; } = float.MinValue;
        public bool WasDamagedRecently => LastTimeDamaged + recentDamageTimeout >= Time.time;

        public void GotDamaged()
        {
            LastTimeDamaged = Time.time;
        }

        public void Reset()
        {
            LastTimeDamaged = float.MinValue;
        }
    }
}