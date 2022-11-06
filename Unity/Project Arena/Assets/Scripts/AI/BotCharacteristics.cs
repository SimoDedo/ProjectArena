using System;
using System.Runtime.Serialization;
using System.Text;
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

        [DataMember] [Range(0.5f, 1.5f)] public readonly float curiosity = 0.5f;// TODO understand if we can turn this into something continuous

        // Parameters not influenced by general skill
        [DataMember] public readonly float speed = 16f;
        
        [JsonConverter(typeof(StringEnumConverter))]
        [DataMember] public readonly Recklessness recklessness = Recklessness.Neutral;

        [DataMember] [Range(30f, 90f)] public readonly float fov = 60f; // TODO maybe this can be slightly influenced by skill (+-10% ?)

        [DataMember] public readonly float maxRange = 100f; //TODO same as above?

        [DataMember] public readonly float damageTimeout = 0.3f;
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
        // TODO Define max and min depending on bot generalSkill and specific ability to help you 
        // better control the influence of the ability scores.
        public float CameraSpeed => Interpolate(MIN_EYE_SPEED, MAX_EYE_SPEED, ScaleScore(generalSkill,c.eyeSpeed));
        public float CameraAcceleration => Interpolate(MIN_EYE_SPEED, MAX_EYE_SPEED, ScaleScore(generalSkill,c.eyeSpeed));
        public float MemoryWindow => Interpolate(MIN_MEMORY_WINDOW, MAX_MEMORY_WINDOW, ScaleScore(generalSkill,c.reflex));
        public float DetectionWindow => Interpolate(MIN_DETECTION_WINDOW, MAX_DETECTION_WINDOW, ScaleScore(generalSkill,c.reflex));
        public float TimeBeforeReaction => Interpolate(MIN_TIME_BEFORE_REACTION, MAX_TIME_BEFORE_REACTION, 1.0f - ScaleScore(generalSkill,c.reflex));
        public float Prediction => Interpolate(MIN_PREDICTION, MAX_PREDICTION, ScaleScore(generalSkill, c.prediction));
        public float AimDelayAverage => Interpolate(MIN_AIM_DELAY_AVERAGE, MAX_AIM_DELAY_AVERAGE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float AimingDispersionAngle => Interpolate(MIN_AIM_DISPERSION_ANGLE, MAX_AIM_DISPERSION_ANGLE, 1.0f - ScaleScore(generalSkill, c.aiming));
        public float Speed => c.speed;

        public float FOV
        {
            get
            {
                var percentage = (generalSkill - 0.5f) / 5; // +- 10%
                return c.fov + c.fov * percentage;
            }
        }
        public float StandStillInFightProbability => Interpolate(MIN_STAND_STILL_IN_FIGHT, MAX_STAND_STILL_IN_FIGHT, 1.0f - ScaleScore(generalSkill, c.movementSkill));
        public float RandomlyMoveInFightProbability => Interpolate(MIN_RANDOM_MOVE_IN_FIGHT, MAX_RANDOM_MOVE_IN_FIGHT, 1.0f - ScaleScore(generalSkill, c.movementSkill));
        public float DodgeRocketProbability => Interpolate(MIN_DODGE_ROCKET_PROBABILITY, MAX_DODGE_ROCKET_PROBABILITY, ScaleScore(generalSkill, c.movementSkill));
        public float CanSelectCoverProbability => Interpolate(MIN_CAN_SELECT_COVER_PROBABILITY, MAX_CAN_SELECT_COVER_PROBABILITY, ScaleScore(generalSkill, c.movementSkill));

        public CuriosityLevel Curiosity
        {
            get
            {
                var score = ScaleScore(generalSkill, c.curiosity);
                if (score < 0.3f)
                {
                    return CuriosityLevel.Low;
                }

                if (score < 0.7f)
                {
                    return CuriosityLevel.Medium;
                }

                return CuriosityLevel.High;
            }
        }
        public Recklessness Recklessness => c.recklessness;
        public float MaxRange => c.maxRange;
        public float DamageTimeout => c.damageTimeout;

        /// <summary>
        /// Default characteristics of a bot. Average abilities all around.
        /// </summary>
        public static BotCharacteristics Default =>
            new BotCharacteristics(
                0.5f,
                new JSonBotCharacteristics()
            );


        private const float MIN_EYE_SPEED = 500f;
        private const float MAX_EYE_SPEED = 5000f;
        private const float MIN_MEMORY_WINDOW = 2f;
        private const float MAX_MEMORY_WINDOW = 5f;
        private const float MIN_DETECTION_WINDOW = 1f;
        private const float MAX_DETECTION_WINDOW = 3f;
        private const float MIN_TIME_BEFORE_REACTION = 0.05f;
        private const float MAX_TIME_BEFORE_REACTION = 0.5f;
        private const float MIN_PREDICTION = 0.1f;
        private const float MAX_PREDICTION = 0.8f;
        private const float MIN_STAND_STILL_IN_FIGHT = 0.1f;
        private const float MAX_STAND_STILL_IN_FIGHT = 1.0f;
        private const float MIN_RANDOM_MOVE_IN_FIGHT = -0.0f;
        private const float MAX_RANDOM_MOVE_IN_FIGHT = 1.0f;
        private const float MIN_DODGE_ROCKET_PROBABILITY = 0.2f;
        private const float MAX_DODGE_ROCKET_PROBABILITY = 1.3f;
        private const float MIN_CAN_SELECT_COVER_PROBABILITY = -0.3f;
        private const float MAX_CAN_SELECT_COVER_PROBABILITY = 1.0f;
        private const float MIN_AIM_DELAY_AVERAGE = -0.025f;
        private const float MAX_AIM_DELAY_AVERAGE = 0.05f;
        private const float AIM_DELAY_STD_DEV = 0.06f;
        private const float MIN_AIM_DISPERSION_ANGLE = 2f;
        private const float MAX_AIM_DISPERSION_ANGLE = 30f;

        // With these params, at best an entity will aim at the enemy position ~80% of the times, at
        // worst ~5% of the times. 
        // Check from (example) WolframAlpha: 
        // integrate 1/(sqrt(2*pi)*s)*e^(-(x-m)^2/(2*s^2)) from -infinity to 0 with s = 0.06 and m = <mean>
        
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
    /// Determines how the bot is able to move when fighting an enemy.
    /// </summary>
    public enum FightingMovementSkill
    {
        StandStill, // Cannot move and aim at the same time.
        MoveStraight, // Can move, but only toward / away from the target in a straight line.
        CircleStrife, // Can strife around the target, but only in one direction.
        CircleStrifeChangeDirection // Can strife around the target, changing direction if necessary.
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
        Low, // The bot prefers avoiding fights, so it will look for cover and pickup more.
        Neutral, // The bot is not overly cautious or reckless.
        High // The bot prefers to fight head on, hardly using cover or retreating for pickups.
    }
}

