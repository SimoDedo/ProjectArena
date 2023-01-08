using System;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

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
        private const float TIMEOUT_CHANGE_IDEA = 1.5f; 
        private float timestampChangeIdeaAllowed; 
        private float probabilityFightBack;
        private bool hasDecidedToWatchOut;
        
        private string nameToRemove;
        public override void OnAwake()
        {
            var entity = GetComponent<AIEntity>();
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
            probabilityFightBack = entity.Characteristics.FightBackWhenCollectingPickup;

            nameToRemove = entity.name;
        }

        public override void OnStart()
        {
            hasDecidedToWatchOut = false;
        }

        public override TaskStatus OnUpdate()
        {
            if (Time.time < timestampChangeIdeaAllowed)
            {
                // I won't change my decision for now, do not fight
                return hasDecidedToWatchOut ? TaskStatus.Success : TaskStatus.Failure;
            }

            hasDecidedToWatchOut = false;
            if (!damageSensor.WasDamagedRecently && !soundSensor.HeardShotRecently) return TaskStatus.Failure;

            timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
            if (Random.value > probabilityFightBack)
            {
                // Debug.Log(nameToRemove + ": I feel the enemy, but I'm not cutting it...");
                // Not feeling enough confident to look around.
                // timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
                return TaskStatus.Failure;
            }

            // Debug.Log(nameToRemove + ": I feel the enemy, searching...");
            hasDecidedToWatchOut = true;
            return TaskStatus.Success;
        }
    }
}