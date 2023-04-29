using System;
using AI.Layers.SensingLayer;
using BehaviorDesigner.Runtime.Tasks;
using Bonsai;
using Bonsai.CustomNodes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.BonsaiBehaviours.Conditions
{
    /// <summary>
    /// Returns Success if we have taken damage recently and should react about it, Failure otherwise.
    /// </summary>
    [BonsaiNode("Conditional/")]
    public class ShouldWatchOutForEnemy : AutoConditionalAbort
    {
        private DamageSensor damageSensor;
        private SoundSensor soundSensor;
        private const float TIMEOUT_CHANGE_IDEA = 1.5f;
        private float timestampChangeIdeaAllowed;
        private float probabilityFightBack;
        private bool hasDecidedToWatchOut;

        public override void OnStart()
        {
            var entity = Actor.GetComponent<AIEntity>();
            damageSensor = entity.DamageSensor;
            soundSensor = entity.SoundSensor;
            probabilityFightBack = entity.Characteristics.FightBackWhenCollectingPickup;
        }

        public override void OnEnter()
        {
            hasDecidedToWatchOut = false;
        }

        public override bool Condition()
        {
            if (Time.time < timestampChangeIdeaAllowed)
            {
                // I won't change my decision for now, do not fight
                return hasDecidedToWatchOut;
            }

            hasDecidedToWatchOut = false;
            if (!damageSensor.WasDamagedRecently && !soundSensor.HeardShotRecently) return false;

            timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
            if (Random.value > probabilityFightBack)
            {
                // Debug.Log(nameToRemove + ": I feel the enemy, but I'm not cutting it...");
                // Not feeling enough confident to look around.
                // timestampChangeIdeaAllowed = Time.time + TIMEOUT_CHANGE_IDEA;
                return false;
            }

            // Debug.Log(nameToRemove + ": I feel the enemy, searching...");
            hasDecidedToWatchOut = true;
            return true;
        }

        public override Status Run()
        {
            return Condition() ? Status.Success : Status.Failure;
        }
    }
}