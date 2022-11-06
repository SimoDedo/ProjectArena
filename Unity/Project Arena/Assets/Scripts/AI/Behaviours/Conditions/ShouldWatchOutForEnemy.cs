using System;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace AI.Behaviours.Conditions
{
    /// <summary>
    /// Returns Success if we have taken damage recently and should react about it, Failure otherwise.
    /// </summary>
    [Serializable]
    public class ShouldWatchOutForEnemy : Conditional
    {
        private DamageSensor damageSensor;
        private SoundSensor soundSensor;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
        }

        public override TaskStatus OnUpdate()
        {
            // TODO add possibility to not watch out depending on some parameters (Recklessness?)
            return damageSensor.WasDamagedRecently || soundSensor.HeardShotRecently
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }
    }
}