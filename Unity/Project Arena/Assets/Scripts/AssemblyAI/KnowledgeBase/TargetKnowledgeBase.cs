using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyAI.AI.Layer1.Sensors;
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
    /// It's possible to reduce the amount of time required to spot someone by "focusing".
    /// Focusing lasts for Y seconds and it's particularly useful if we lost track of the enemy recently in
    /// order to react faster to its presence.
    ///
    /// It's also possible to manually mark the enemy as found (if, for example, we take some damage and we want
    /// to immediately know it's position)
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
            public bool isVisible;
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
        /// If true, the non consecutive time to spot an entity is reduced according to FOCUSED_MULTIPLIER.
        /// </summary>
        private bool isFocused;

        /// <summary>
        /// Amount of time after which the focused condition expires automatically
        /// </summary>
        private float focusedExpirationTime;

        private const float FOCUSED_MULTIPLIER = 0.3f;
        private const float FOCUSED_DEFAULT_TIMEOUT = 4f;

        /// <summary>
        /// List of visibility data gathered so far, excluding data older than memoryWindow
        /// </summary>
        private List<VisibilityInInterval> results = new List<VisibilityInInterval>();

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
            var isTargetClose = target.isAlive && (targetTransform.position - me.transform.position).sqrMagnitude < 10;
            var result = isTargetClose || sensor.CanSeeObject(targetTransform, Physics.DefaultRaycastLayers);
            if (results.Count != 0)
            {
                var last = results.Last();
                last.endTime = Time.time;
                if (last.isVisible != result)
                    results.Add(
                        new VisibilityInInterval {isVisible = result, startTime = Time.time, endTime = Time.time}
                    );
            }
            else
            {
                results.Add(new VisibilityInInterval {isVisible = result, startTime = Time.time, endTime = Time.time});
            }

            if (isFocused && focusedExpirationTime < Time.time)
            {
                isFocused = false;
            }

            ForgetOldData();
        }

        public void ApplyFocus(float timeout = FOCUSED_DEFAULT_TIMEOUT)
        {
            isFocused = true;
            focusedExpirationTime = Time.time + timeout;
        }

        public void RemoveFocus()
        {
            isFocused = false;
        }

        public bool HasSeenTarget()
        {
            if (!target.isAlive) return false;
            return TestDetection(
                Time.time - detectionWindow,
                Time.time,
                isFocused
            );
        }

        public bool HasLostTarget()
        {
            if (!target.isAlive) return false;
            if (HasSeenTarget())
            {
                return false;
            }

            return TestDetection(
                Time.time - memoryWindow,
                Time.time - detectionWindow,
                false
            );
        }

        public float GetLastSightedTime()
        {
            var searchTimeEnd = Time.time;
            var result = results.FindLast(it => it.isVisible && it.startTime < searchTimeEnd);
            if (result == null)
            {
                // We don't really know the position of the target, but maybe we got damaged and we want
                // magically get to know the latest position of the enemy.
                // To avoid outright cheating, when I get damaged I can give an estimate on 
                // the enemy position based on its real position (e.g. a circle around that)
                return float.NaN;
            }

            return result.endTime;
        }

        private void ForgetOldData()
        {
            // Remove all data which is too old
            results = results.Where(it => it.endTime > Time.time - memoryWindow).ToList();
            // "Forget" (aka clamp) measurements to the memory window interval
            var first = results.First();
            first.startTime = Mathf.Max(first.startTime, Time.time - memoryWindow);
        }


        private bool TestDetection(
            float beginTime,
            float endTime,
            bool fasterReactionTime
        )
        {
            var reactionTime = fasterReactionTime
                ? nonConsecutiveTimeBeforeReaction * FOCUSED_MULTIPLIER
                : nonConsecutiveTimeBeforeReaction;
            var totalTimeVisible = 0f;

            for (var i = results.Count - 1; i >= 0; i--)
            {
                var t = results[i];
                if (t.endTime < beginTime) continue;
                if (t.startTime > endTime) continue;

                var windowLenght = Math.Min(t.endTime, endTime) -
                    Math.Max(t.startTime, beginTime);

                if (t.isVisible)
                {
                    totalTimeVisible += windowLenght;
                    if (totalTimeVisible > reactionTime) return true;
                }
            }

            return false;
        }
    }
}