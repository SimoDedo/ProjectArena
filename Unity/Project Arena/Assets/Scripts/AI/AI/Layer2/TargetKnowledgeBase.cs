using System;
using System.Collections.Generic;
using System.Linq;
using AI.AI.Layer1;
using UnityEngine;

namespace AI.AI.Layer2
{
    /// <summary>
    /// This component keeps track of when the provided target was visibile and when we should consider they
    /// as spotted or lost.
    ///
    /// Detection or loss of the enemy is based around the following concepts:
    /// - Memory window: interval of time, starting from the current instant and going back, for which detection
    ///   events are remembered. Older events are discarded;
    /// - Detection window: interval of time, starting from the current instant and going back, for which sighting
    ///   of the enemy contributes to determining if the enemy is actually detected or not.
    /// - Detection: An enemy is detected if it is sight during the detection window for a sufficient amount of time.
    ///   Each sight event has a weight based on how close the enemy was (the closer, the more relevant the sighting).
    /// - Loss: An enemy is lost if if is not detected considering only the events in the detection window, but it is
    ///   detected when considering the whole memory window.
    /// </summary>
    
    // TODO maybe separate responsibility of enemy knowledge vs extraction of info (e.g. can react to enemy).
    public class TargetKnowledgeBase
    {
        private static readonly AnimationCurve DistanceScore = new AnimationCurve(
            new Keyframe(0f, 5f),
            new Keyframe(10f, 3f),
            new Keyframe(30f, 1.5f),
            new Keyframe(50f, 1f)
        );

        /// <summary>
        /// Length of the detection window (in seconds).
        /// </summary>
        private readonly float detectionWindowLenght;

        // The entity this component belongs to.
        private readonly AIEntity me;

        // Lenght of the memory window (in seconds).
        private readonly float memoryWindowLength;

        // Total time (in the detection window) that the enemy must be seen in the detection interval before declaring
        // tha we can detect it.
        private readonly float nonConsecutiveTimeBeforeReaction;

        // The target that must be spotted
        private readonly Entity.Entity target;

        // List of visibility data gathered so far, excluding data older than memoryWindow
        private readonly List<VisibilityInInterval> results = new List<VisibilityInInterval>();

        /// The sensor used to detect the target presence
        private SightSensor sensor;

        public TargetKnowledgeBase(
            AIEntity me,
            Entity.Entity target,
            float memoryWindowLength,
            float detectionWindowLenght,
            float nonConsecutiveTimeBeforeReaction
        )
        {
            this.target = target;
            this.memoryWindowLength = memoryWindowLength;
            this.detectionWindowLenght = detectionWindowLenght;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
            this.me = me;
        }

        /// <summary>
        /// Stores the last time the enemy was considered detected.
        /// </summary>
        public float LastTimeDetected { get; private set; } = float.MinValue;

        // Finishes preparing the componet.
        public void Prepare()
        {
            sensor = me.SightSensor;
        }

        /// <summary>
        /// Updates the target knowledge base by checking if the target is sighted or not.
        /// </summary>
        public void Update()
        {
            var targetTransform = target.transform;
            // TODO Understand if this can be removed to avoid magically knowing enemy position when sneaking behind
            var isTargetClose = target.IsAlive && (targetTransform.position - me.transform.position).sqrMagnitude < 10;
            var result = isTargetClose || sensor.CanSeeObject(targetTransform);

            var score = 0f;
            if (result)
            {
                score = DistanceScore.Evaluate((me.transform.position - targetTransform.position).magnitude);
                results.Add(new VisibilityInInterval
                    {visibilityScore = score, startTime = Time.time - Time.deltaTime, endTime = Time.time});
            }
            else
            {
                VisibilityInInterval last;
                if (results.Count != 0 && (last = results.Last()).visibilityScore == 0)
                    last.endTime = Time.time;
                else
                    results.Add(new VisibilityInInterval
                        {visibilityScore = score, startTime = Time.time - Time.deltaTime, endTime = Time.time});
            }


            ForgetOldData();

            if (HasSeenTarget()) LastTimeDetected = Time.time;
        }

        /// <summary>
        /// Forgets all detection data.
        /// </summary>
        public void Reset()
        {
            results.Clear();
            LastTimeDetected = float.MinValue;
        }

        /// <summary>
        /// Returns whether we consider the target as detected.
        /// </summary>
        public bool HasSeenTarget()
        {
            if (!target.IsAlive)
            {
                // If the enemy is not alive, immediately stop considering it as detected.
                return false;
            }
            return TestDetection(Time.time - detectionWindowLenght, Time.time);
        }


        /// <summary>
        /// Returns whether we consider the target as lost.
        /// </summary>
        public bool HasLostTarget()
        {
            if (!target.IsAlive) return false;
            return !HasSeenTarget() && TestDetection(Time.time - memoryWindowLength, Time.time - detectionWindowLenght);
        }

        // Forgets data which doesn't fit the memory window.
        private void ForgetOldData()
        {
            // Remove all data which is too old
            var firstIndexToKeep = results.FindIndex(it => it.endTime > Time.time - memoryWindowLength);
            if (firstIndexToKeep == -1)
            {
                // Nothing to keep
                results.Clear();
                return;
            }

            results.RemoveRange(0, firstIndexToKeep);
            var first = results.First();
            first.startTime = Mathf.Max(first.startTime, Time.time - memoryWindowLength);
        }


        // Tests detection in the interval specified.
        private bool TestDetection(float beginTime, float endTime)
        {
            var totalTimeVisible = 0f;

            for (var i = results.Count - 1; i >= 0; i--)
            {
                var t = results[i];
                if (t.endTime < beginTime) continue;
                if (t.startTime > endTime) continue;

                var windowLenght = Math.Min(t.endTime, endTime) -
                                   Math.Max(t.startTime, beginTime);

                totalTimeVisible += windowLenght * t.visibilityScore;
                if (totalTimeVisible > nonConsecutiveTimeBeforeReaction) return true;
            }

            return false;
        }

        /// <summary>
        /// Represents whether the target was visible during the interval specified.
        /// </summary>
        private class VisibilityInInterval
        {
            public float endTime; // Exclusive
            public float startTime; // Inclusive
            public float visibilityScore;
        }
    }
}