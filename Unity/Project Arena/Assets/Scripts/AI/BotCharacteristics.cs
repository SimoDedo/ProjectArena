using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace AI
{
    [DataContract]
    public class JSonBotCharacteristics
    {
        // Parameters influenced by general skill
        [DataMember] [Range(0.5f, 1.5f)] public readonly float eyeSpeed = 1.0f;
        
        [DataMember] [Range(0.5f, 1.5f)] public readonly float reflex = 1.0f;
        
        [DataMember] [Range(0.5f, 1.5f)] public readonly float prediction = 1.0f;
        
        [DataMember] [Range(0.5f, 1.5f)] public readonly float aiming = 1.0f;
        
        [DataMember] [Range(0.5f, 1.5f)] public readonly float movementSkill = 0.5f;

        [DataMember] [Range(0.5f, 1.5f)] public readonly float curiosity = 0.5f;
        
        // Parameters not influenced by general skill
        [DataMember] public readonly float speed = 16f;
        
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember] public readonly Recklessness recklessness = Recklessness.Neutral;

        [DataMember] [Range(30f, 90f)] public readonly float fov = 60f;

        [DataMember] public readonly float maxRange = 100f; 

        // Amount of time, measured from the first moment we can react to an enemy event (e.g. heard sound, got damaged),
        //  after which we "forget" about the event
        [DataMember] public readonly float eventReactionTimeout = 0.3f;
    }

    [Serializable]
    public class BotCharacteristics
    {
        [Range(0.0f, 1.0f)] public readonly float generalSkill;

        private JSonBotCharacteristics c;

        public BotCharacteristics(float generalSkill, JSonBotCharacteristics characteristics)
        {
            this.generalSkill = generalSkill;
            c = characteristics;
        }

        // Abilities directly usable by bot
        public float CameraSpeed => Interpolate(MIN_EYE_SPEED, MAX_EYE_SPEED, ScaleScore(generalSkill,c.eyeSpeed));
        public float CameraAcceleration => Interpolate(MIN_EYE_SPEED, MAX_EYE_SPEED, ScaleScore(generalSkill,c.eyeSpeed));
        public float MemoryWindow => Interpolate(MIN_MEMORY_WINDOW, MAX_MEMORY_WINDOW, ScaleScore(generalSkill,c.reflex));
        public float DetectionWindow => Interpolate(MIN_DETECTION_WINDOW, MAX_DETECTION_WINDOW, ScaleScore(generalSkill,c.reflex));
        public float TimeBeforeReaction => Interpolate(MIN_TIME_BEFORE_REACTION, MAX_TIME_BEFORE_REACTION, 1.0f - ScaleScore(generalSkill,c.reflex));
        public float Prediction => Interpolate(MIN_PREDICTION, MAX_PREDICTION, ScaleScore(generalSkill, c.prediction));
        public float SpawnPointPrediction => Interpolate(MIN_SPAWN_PREDICTION, MAX_SPAWN_PREDICTION, ScaleScore(generalSkill, c.prediction));
        public float UncorrectableAimDelayAverage => Interpolate(MIN_UNCORRECTABLE_AIM_DELAY_AVERAGE, MAX_UNCORRECTABLE_AIM_DELAY_AVERAGE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float CorrectableAimDelayAverage => Interpolate(MIN_CORRECTABLE_AIM_DELAY_AVERAGE, MAX_CORRECTABLE_AIM_DELAY_AVERAGE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float AimingDispersionAngle => Interpolate(MIN_AIM_DISPERSION_ANGLE, MAX_AIM_DISPERSION_ANGLE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float AcceptableShootingAngle => Interpolate(MIN_ACCEPTABLE_SHOOTING_ANGLE, MAX_ACCEPTABLE_SHOOTING_ANGLE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float Speed => c.speed;

        public float FOV
        {
            get
            {
                var percentage = (generalSkill - 0.5f) / 5; // +- 10%
                return c.fov + c.fov * percentage;
            }
        }

        public float FightingSkill => ScaleScore(generalSkill, c.movementSkill);
        public float GunMovementCorrectness => Interpolate(MIN_DEVIATION_FROM_OPTIMAL_RANGE, MAX_DEVIATION_FROM_OPTIMAL_RANGE, 1.0f - ScaleScore(generalSkill, c.movementSkill));
        public float FightBackWhenCollectingPickup => Interpolate(MIN_FIGHT_BACK_WHEN_PICKUP, MAX_FIGHT_BACK_WHEN_PICKUP, ScaleScore(generalSkill, c.movementSkill));
        public float DodgeRocketProbability => Interpolate(MIN_DODGE_ROCKET_PROBABILITY, MAX_DODGE_ROCKET_PROBABILITY, ScaleScore(generalSkill, c.movementSkill));
        public float CanSelectCoverProbability => Interpolate(MIN_CAN_SELECT_COVER_PROBABILITY, MAX_CAN_SELECT_COVER_PROBABILITY, ScaleScore(generalSkill, c.movementSkill));
        public float UnpredictabilityHistoryWeight => Interpolate(WORST_UNPREDICTABLE_DELAY_WEIGHT, BEST_UNPREDICTABLE_DELAY_WEIGHT, ScaleScore(generalSkill, c.aiming));
        
        // TODO understand if we can turn this into something continuous.
        public CuriosityLevel Curiosity
        {
            get
            {
                var score = ScaleScore(generalSkill, c.curiosity);
                return score switch
                {
                    < 0.3f => CuriosityLevel.Low,
                    < 0.7f => CuriosityLevel.Medium,
                    _ => CuriosityLevel.High
                };
            }
        }
        public Recklessness Recklessness => c.recklessness;
        public float MaxRange {
            get
            {
                var percentage = (generalSkill - 0.5f) * 3 / 5; // +- 30%
                return c.maxRange + c.maxRange * percentage;
            }
        }
        public float EventReactionTimeout => c.eventReactionTimeout;
        public float SoundThreshold => Interpolate(MIN_SOUND_THRESHOLD, MAX_SOUND_THRESHOLD, 1.0f - generalSkill);

        /// <summary>
        /// Default characteristics of a bot. Average abilities all around.
        /// </summary>
        public static BotCharacteristics Default => new (
            0.5f,
            new JSonBotCharacteristics()
                );


        private const float MIN_EYE_SPEED = 1200f;
        private const float MAX_EYE_SPEED = 3500f;
        private const float MIN_MEMORY_WINDOW = 2f;
        private const float MAX_MEMORY_WINDOW = 5f;
        private const float MIN_DETECTION_WINDOW = 1f;
        private const float MAX_DETECTION_WINDOW = 3f;
        private const float MIN_TIME_BEFORE_REACTION = 0.125f;
        private const float MAX_TIME_BEFORE_REACTION = 0.450f;
        private const float MIN_PREDICTION = 0.1f;
        private const float MAX_PREDICTION = 0.8f;
        private const float MIN_SPAWN_PREDICTION = -0.1f;
        private const float MAX_SPAWN_PREDICTION = 0.4f;
        private const float MIN_DEVIATION_FROM_OPTIMAL_RANGE = 5.0f;
        private const float MAX_DEVIATION_FROM_OPTIMAL_RANGE = 20.0f;
        private const float MIN_FIGHT_BACK_WHEN_PICKUP = -0.5f;
        private const float MAX_FIGHT_BACK_WHEN_PICKUP = 1.5f;
        private const float MIN_DODGE_ROCKET_PROBABILITY = 0.2f;
        private const float MAX_DODGE_ROCKET_PROBABILITY = 1.3f;
        private const float MIN_CAN_SELECT_COVER_PROBABILITY = -0.3f;
        private const float MAX_CAN_SELECT_COVER_PROBABILITY = 0.6f;

        private const float MIN_UNCORRECTABLE_AIM_DELAY_AVERAGE = 0.001f;
        private const float MAX_UNCORRECTABLE_AIM_DELAY_AVERAGE = 0.070f;
        private const float UNCORRECTABLE_AIM_DELAY_STD_DEV = 0.03f;
        // private const float UNCORRECTABLE_AIM_DELAY_STD_DEV = 0.06f;

        // private const float MIN_UNCORRECTABLE_AIM_DELAY_AVERAGE = -0.015f;
        // private const float MAX_UNCORRECTABLE_AIM_DELAY_AVERAGE = 0.06f;
        // private const float UNCORRECTABLE_AIM_DELAY_STD_DEV = 0.06f;

        private const float MIN_CORRECTABLE_AIM_DELAY_AVERAGE = 0.3f;
        private const float MAX_CORRECTABLE_AIM_DELAY_AVERAGE = 0.5f;

        private const float WORST_UNPREDICTABLE_DELAY_WEIGHT = 0.15f;
        private const float BEST_UNPREDICTABLE_DELAY_WEIGHT = 0.5f;
        
        private const float MIN_AIM_DISPERSION_ANGLE = 4f;
        private const float MAX_AIM_DISPERSION_ANGLE = 20f;
        private const float MIN_ACCEPTABLE_SHOOTING_ANGLE = 0.01f;
        private const float MAX_ACCEPTABLE_SHOOTING_ANGLE = 0.3f;

        private const float MIN_SOUND_THRESHOLD = 0.075f;
        private const float MAX_SOUND_THRESHOLD = 0.2f;
        
        
        private static float ScaleScore(float general, float specific)
        {
            return Mathf.Clamp(general * specific, 0.0f, 1.0f);
        }

        private static float Interpolate(float min, float max, float percentage)
        {
            return min + percentage * (max - min);
        }

        public override string ToString()
        {
            // var builder = new StringBuilder();
            //
            // return builder.ToString();
            return JsonConvert.SerializeObject(this);
        }
    }

    /// <summary>
    /// Determines how much the bot tends to look around when moving.
    /// </summary>
    public enum CuriosityLevel
    {
        Low, // The bot only looks forward.
        Medium, // The bot looks forward and at the sides.
        High // The bot looks all around itself.
    }

    /// <summary>
    /// Determines if the enemy tends to be cautious or dives head-on into battle.
    /// </summary>
    public enum Recklessness
    {
        Low, // The bot prefers avoiding fights, so it will look for cover and pickups more.
        Neutral, // The bot is not overly cautious or reckless.
        High // The bot prefers to fight head on, hardly using cover or retreating for pickups.
    }
}

