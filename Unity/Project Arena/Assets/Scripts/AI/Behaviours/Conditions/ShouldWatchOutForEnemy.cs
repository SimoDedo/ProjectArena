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
        private FightingMovementSkill skill;

        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            skill = entity.MovementSkill;
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
        }

        public override TaskStatus OnUpdate()
        {
            if (skill == FightingMovementSkill.StandStill)
                // Never fight back if I don't have the required movement skill!
                return TaskStatus.Failure;

            Debug.Log(gameObject.name + " will watch out? " + soundSensor.HeardShotRecently);
            
            return damageSensor.WasDamagedRecently || soundSensor.HeardShotRecently
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }
    }
}