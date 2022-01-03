using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyLogging;
using Entities.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.KnowledgeBase
{
    // How is the target considered visible?
    // At every update or whenever requested, the kb calculates the memory window, that is, the time window
    // in which we should consider whether we have seen the enemy or not. 
    // Starting from the end of this window (aka starting from the most recent event), we accumulate the time the enemy
    // was seen or not seen in the window. If the first accumulator going above the time detection threshold is the one
    // for when the enemy is visible, then the enemy is reported as visible, otherwise the bot shouldn't react to the
    // enemy presence (even if, currently, it might be visible).
    // Possible improvements: If the enemy is very close/very far, increase/decrease the weight of the observation.
    // AKA assign a score to each observation / non observation event based on lenght and distance.

    public class TargetKnowledgeBase : MonoBehaviour
    {
        private class VisibilityData
        {
            public float startTime;
            public float endTime;
            public bool isVisible;
        }

        private Entity target;
        private int entityID;
        private AISightSensor sensor;
        private float memoryWindow;

        /// <summary>
        /// Total time (in the memory window) that the enemy must be seen or not seen before declaring that
        /// we can detect it or have lost it.
        /// </summary>
        private float nonConsecutiveTimeBeforeReaction;

        private List<VisibilityData> results = new List<VisibilityData>();

        public void Prepare(
            AISightSensor sensor,
            Entity target,
            float memoryWindow,
            float nonConsecutiveTimeBeforeReaction,
            int entityID
        )
        {
            this.target = target;
            this.sensor = sensor;
            this.memoryWindow = memoryWindow;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
            this.entityID = entityID;
        }

        private void Update()
        {
            var enemyTransform = target.transform;
            var isTargetClose = target.isAlive && (enemyTransform.position - transform.position).sqrMagnitude < 10;
            var result = isTargetClose || sensor.CanSeeObject(enemyTransform, Physics.DefaultRaycastLayers);
            if (results.Count != 0)
            {
                var last = results.Last();
                last.endTime = Time.time;
                if (last.isVisible != result)
                {
                    results.Add(new VisibilityData {isVisible = result, startTime = Time.time, endTime = Time.time});
                }
            }
            else
            {
                results.Add(new VisibilityData {isVisible = result, startTime = Time.time, endTime = Time.time});
            }

            CompactList();
        }

        private void CompactList()
        {
            // Remove all data which is too old
            results = results.Where(it => it.endTime > Time.time - memoryWindow).ToList();
            // "Forget" (aka clamp) measurements to the memory window interval
            var first = results.First();
            first.startTime = Mathf.Max(first.startTime, Time.time - memoryWindow);
        }


        private bool previouslyVisible;

        public bool HasSeenTarget(bool fastReact = false)
        {
            //Force updating since the enemy might have changed position since this component last update
            // or update might not have been called yet
            Update();

            var visible = TestDetection(fastReact);
            if (visible != previouslyVisible)
            {
                previouslyVisible = visible;
                if (visible)
                    EnemyInSightGameEvent.Instance.Raise(entityID);
                else
                    EnemyOutOfSightGameEvent.Instance.Raise(entityID);
            }

            return visible;
        }

        private bool TestDetection(bool fasterReactionTime)
        {
            var reactionTime = fasterReactionTime
                ? nonConsecutiveTimeBeforeReaction * 0.3
                : nonConsecutiveTimeBeforeReaction;
            var beginWindow = Time.time - memoryWindow;
            var endWindow = Time.time;
            var totalTimeVisible = 0f;
            var totalTimeNotVisible = 0f;

            for (var i = results.Count - 1; i >= 0; i--)
            {
                var t = results[i];
                if (t.endTime < beginWindow) continue;
                if (t.startTime > endWindow) continue;

                var windowLenght = Math.Min(t.endTime, endWindow) -
                    Math.Max(t.startTime, beginWindow);

                if (t.isVisible)
                {
                    totalTimeVisible += windowLenght;
                    if (totalTimeVisible > reactionTime) return true;
                }
                else
                {
                    totalTimeNotVisible += windowLenght;
                    if (totalTimeNotVisible > reactionTime) return false;
                }
            }

            return false;
        }

        public float GetLastKnownPositionTime()
        {
            var searchTimeEnd = Time.time;
            var result = results.FindLast(it => it.isVisible && it.startTime < searchTimeEnd);
            // We got here not because we lost track of target, but for other reasons (e.g. got damage),
            // return current position of enemy
            return result?.startTime ?? Time.time;
        }

        public void SetActive(bool active)
        {
            if (!active)
            {
                results.Clear();
            }
            enabled = false;
        }
    }
}