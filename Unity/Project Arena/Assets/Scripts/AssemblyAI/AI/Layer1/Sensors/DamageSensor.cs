using UnityEngine;

namespace AssemblyAI.AI.Layer1.Sensors
{
    public class DamageSensor
    {
        private readonly float recentDamageTimeout;
        public DamageSensor(float recentDamageTimeout)
        {
            this.recentDamageTimeout = recentDamageTimeout;
        }

        public void GotDamaged()
        {
            LastTimeDamaged = Time.time;
        }

        public void Reset()
        {
            LastTimeDamaged = float.MinValue;
        }
        
        public float LastTimeDamaged { get; private set; } = float.MinValue;
        public bool WasDamagedRecently => LastTimeDamaged + recentDamageTimeout >= Time.time;
    }
}