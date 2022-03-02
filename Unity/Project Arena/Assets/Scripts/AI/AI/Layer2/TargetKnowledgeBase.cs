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
        public readonly struct EnemyInfo
        {
            public readonly bool isVisible;
            public readonly float distance;
            public readonly float startTime;
            public readonly float endTime;

            public EnemyInfo(bool isVisible, float distance, float startTime, float endTime)
            {
                this.isVisible = isVisible;
                this.distance = distance;
                this.startTime = startTime;
                this.endTime = endTime;
            }

            public EnemyInfo CreateWithAdjustedStartTime(float newStartTime)
            {
                return new EnemyInfo(isVisible, distance, newStartTime, endTime);
            }
        }

        // The entity this component belongs to.
        private readonly AIEntity me;

        // Lenght of the memory window (in seconds).
        private readonly float memoryWindowLength;

        // List of visibility data gathered so far, excluding data older than memoryWindow
        private readonly List<EnemyInfo> results = new List<EnemyInfo>();

        // The target that must be spotted
        private readonly Entity.Entity target;

        /// The sensor used to detect the target presence
        private SightSensor sensor;

        public TargetKnowledgeBase(
            AIEntity me,
            Entity.Entity target,
            float memoryWindowLength
        )
        {
            this.target = target;
            this.memoryWindowLength = memoryWindowLength;
            this.me = me;
        }
        
        // Finishes preparing the component.
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
            var targetDistance = (targetTransform.position - me.transform.position).magnitude;
            var isTargetClose = target.IsAlive && targetDistance < 10;
            var result = isTargetClose || sensor.CanSeeObject(targetTransform);

            results.Add(new EnemyInfo(result, targetDistance,  Time.time - Time.deltaTime, Time.time));
            ForgetOldData();
        }

        /// <summary>
        /// Forgets all detection data.
        /// </summary>
        public void Reset()
        {
            results.Clear();
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
            results[0] = first.CreateWithAdjustedStartTime(Mathf.Max(first.startTime, Time.time - memoryWindowLength));
        }

        public List<EnemyInfo> GetEnemyInfo()
        {
            return results;
        }
    }
}