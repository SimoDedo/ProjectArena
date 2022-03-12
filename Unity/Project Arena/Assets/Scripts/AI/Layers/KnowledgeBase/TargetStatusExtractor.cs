using System;
using AI.Layers.Memory;
using UnityEngine;

namespace AI.Layers.KnowledgeBase
{
    public class TargetKnowledgeBase
    {
        private static readonly AnimationCurve DistanceScore = new AnimationCurve(
            new Keyframe(0f, 5f),
            new Keyframe(10f, 3f),
            new Keyframe(30f, 1.5f),
            new Keyframe(50f, 1f)
        );

        // The entity this component belongs to.
        private readonly AIEntity me;

        // The target we are interested in.
        private readonly Entity.Entity target;

        /// <summary>
        /// Stores the last time the enemy was considered detected.
        /// </summary>
        public float LastTimeDetected { get; private set; } = float.MinValue;

        // Total time (in the detection window) that the enemy must be seen in the detection interval before declaring
        // tha we can detect it.
        private readonly float nonConsecutiveTimeBeforeReaction;

        /// <summary>
        /// Length of the detection window (in seconds).
        /// </summary>
        private readonly float detectionWindowLenght;

        private TargetMemory targetKb;
        
        public TargetKnowledgeBase(AIEntity me, Entity.Entity target,
            float detectionWindowLenght, float nonConsecutiveTimeBeforeReaction)
        {
            this.me = me;
            this.target = target;
            this.detectionWindowLenght = detectionWindowLenght;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
        }

        public void Prepare()
        {
            targetKb = me.TargetKb;
        }

        public void Update()
        {
            if (InternalHasSeenTarget())
            {
                LastTimeDetected = Time.time;
            }
        }

        /// <summary>
        /// Returns whether we consider the target as detected.
        /// </summary>
        public bool HasSeenTarget()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return LastTimeDetected == Time.time;
        }
        
        /// <summary>
        /// Returns whether we consider the target as lost.
        /// </summary>
        public bool HasLostTarget()
        {
            if (!target.IsAlive) return false;
            return !HasSeenTarget() && TestDetection(0, Time.time - detectionWindowLenght);
        }

        /// <summary>
        /// Calculates whether we consider the target as detected.
        /// </summary>
        private bool InternalHasSeenTarget()
        {
            if (!target.IsAlive)
                // If the enemy is not alive, immediately stop considering it as detected.
                return false;
            return TestDetection(Time.time - detectionWindowLenght, Time.time);
        }
        
        // Tests detection in the interval specified.
        private bool TestDetection(float beginTime, float endTime)
        {
            var totalTimeVisible = 0f;
            var results = targetKb.GetEnemyInfo();
            for (var i = results.Count - 1; i >= 0; i--)
            {
                var t = results[i];
                if (t.endTime < beginTime) continue;
                if (t.startTime > endTime) continue;

                var windowLenght = Math.Min(t.endTime, endTime) -
                                   Math.Max(t.startTime, beginTime);

                if (t.isVisible)
                {
                    totalTimeVisible += windowLenght * DistanceScore.Evaluate(t.distance);
                }

                if (totalTimeVisible > nonConsecutiveTimeBeforeReaction) return true;
            }

            return false;
        }
    }
}