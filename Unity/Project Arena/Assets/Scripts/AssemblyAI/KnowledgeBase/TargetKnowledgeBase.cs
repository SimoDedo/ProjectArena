using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyAI.AI.Layer1.Sensors;
using AssemblyLogging;
using UnityEngine;

namespace AI.KnowledgeBase
{
    /// <summary>
    /// The TargetKnowledgeBase should allow us to know when the enemy is spotted and we can react to it or
    /// when the enemy was instead lost.
    /// - Enemy spotted: The enemy was sighted for at least nonConsecutiveTimeBeforeReaction seconds in the last
    ///     memoryWindow seconds
    /// - Enemy lost: The enemy is not currently spotted, but it was X seconds ago.
    ///
    /// </summary>
    public class TargetKnowledgeBase
    {
        /// <summary>
        /// Represents whether the target was visible during the interval specified.
        /// </summary>
        private class VisibilityInInterval
        {
            public float startTime; // Inclusive
            public float endTime; // Exclusive
            public float visibilityScore;
        }

        /// <summary>
        /// The entity this component belongs to
        /// </summary>
        private readonly AIEntity me;

        /// <summary>
        /// The target that must be spotted
        /// </summary>
        private readonly Entity target;

        /// <summary>
        /// The sensor used to detect the target presence
        /// </summary>
        private AISightSensor sensor;

        /// <summary>
        /// Size (in seconds) of the memory of this component. Detection event older than this will be forgotten. 
        /// </summary>
        private readonly float memoryWindow;

        /// <summary>
        /// TODO Define this better!
        /// Tempo massimo in cui andare indietro per cercare avvistamenti del nemico / tempo limite passato il quale
        /// calcolare se abbiamo perso o meno il nemico cercando di vedere se era visibile prima di questo tempo
        /// </summary>
        private readonly float detectionWindow;

        /// <summary>
        /// Total time (in the detection window) that the enemy must be seen before declaring that
        /// we can detect it.
        /// </summary>
        private readonly float nonConsecutiveTimeBeforeReaction;

        /// <summary>
        /// List of visibility data gathered so far, excluding data older than memoryWindow
        /// </summary>
        private List<VisibilityInInterval> results = new List<VisibilityInInterval>();

        public float LastTimeDetected { get; private set; } = float.MinValue;
        
        private static readonly AnimationCurve DistanceScore = new AnimationCurve(
            new Keyframe(0f, 5f),
            new Keyframe(10f, 3f),
            new Keyframe(30f, 1.5f),
            new Keyframe(50f, 1f)
        );

        public TargetKnowledgeBase(
            AIEntity me,
            Entity target,
            float memoryWindow,
            float detectionWindow,
            float nonConsecutiveTimeBeforeReaction
        )
        {
            this.target = target;
            this.memoryWindow = memoryWindow;
            this.detectionWindow = detectionWindow;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
            this.me = me;
        }

        public void Prepare()
        {
            sensor = me.SightSensor;
        }

        public void Update()
        {
            var targetTransform = target.transform;
            // TODO Understand if this can be removed to avoid magically knowing enemy position when sneaking behind
            var isTargetClose = target.IsAlive && (targetTransform.position - me.transform.position).sqrMagnitude < 10;
            var result = isTargetClose || sensor.CanSeeObject(targetTransform, Physics.DefaultRaycastLayers);

            var score = 0f;
            if (result)
            {
                score = DistanceScore.Evaluate((me.transform.position - targetTransform.position).magnitude);
                results.Add(new VisibilityInInterval {visibilityScore = score, startTime = Time.time - Time.deltaTime, endTime = Time.time});
            }
            else
            {
                VisibilityInInterval last;
                if (results.Count != 0 && (last = results.Last()).visibilityScore == 0)
                {
                    last.endTime = Time.time;
                }
                else
                {
                    results.Add(new VisibilityInInterval {visibilityScore = score, startTime = Time.time - Time.deltaTime, endTime = Time.time});
                }               
            }


            ForgetOldData();

            if (HasSeenTarget())
            {
                LastTimeDetected = Time.time;
            }
        }

        public void Reset()
        {
            results.Clear();
        }

        public bool HasSeenTarget()
        {
            if (!target.IsAlive) return false;
            return TestDetection(
                Time.time - detectionWindow,
                Time.time
            );
        }

        public bool HasLostTarget()
        {
            if (!target.IsAlive) return false;
            return !HasSeenTarget() && TestDetection(Time.time - memoryWindow, Time.time - detectionWindow);
        }
        
        private void ForgetOldData()
        {
            // Remove all data which is too old
            var firstIndexToKeep = results.FindIndex(it => it.endTime > Time.time - memoryWindow);
            if (firstIndexToKeep == -1)
            {
                // Nothing to keep
                results.Clear();
                return;
            }
            
            results.RemoveRange(0, firstIndexToKeep);
            var first = results.First();
            first.startTime = Mathf.Max(first.startTime, Time.time - memoryWindow);
        }


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
    }
}