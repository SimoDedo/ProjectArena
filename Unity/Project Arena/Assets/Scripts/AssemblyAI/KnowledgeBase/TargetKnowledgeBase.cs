using System;
using System.Collections.Generic;
using System.Linq;
using AssemblyAI.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.KnowledgeBase
{
    public class TargetKnowledgeBase
    {
        private class VisibilityData
        {
            public float startTime;
            public float endTime;
            public bool isVisible;
        }

        private readonly AIEntity me;
        private readonly Entity target;
        private AISightSensor sensor;
        private readonly float memoryWindow;

        /// <summary>
        /// Total time (in the memory window) that the enemy must be seen or not seen before declaring that
        /// we can detect it or have lost it.
        /// </summary>
        private readonly float nonConsecutiveTimeBeforeReaction;

        private bool canReact;
        private bool canReactFast;
        
        private List<VisibilityData> results = new List<VisibilityData>();

        public TargetKnowledgeBase(
            AIEntity me,
            Entity target,
            float memoryWindow,
            float nonConsecutiveTimeBeforeReaction
        )
        {
            this.target = target;
            this.memoryWindow = memoryWindow;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
            this.me = me;
        }

        public void Prepare()
        {
            sensor = me.SightSensor;
        }

        public void Update()
        {
            var enemyTransform = target.transform;
            var isTargetClose = target.isAlive && (enemyTransform.position - me.transform.position).sqrMagnitude < 10;
            var result = isTargetClose || sensor.CanSeeObject(enemyTransform, Physics.DefaultRaycastLayers);
            if (results.Count != 0)
            {
                var last = results.Last();
                last.endTime = Time.time;
                if (last.isVisible != result)
                    results.Add(new VisibilityData {isVisible = result, startTime = Time.time, endTime = Time.time});
            }
            else
            {
                results.Add(new VisibilityData {isVisible = result, startTime = Time.time, endTime = Time.time});
            }

            CompactList();
            canReact = TestDetection(false);
            canReactFast = TestDetection(true);
        }

        private void CompactList()
        {
            // Remove all data which is too old
            results = results.Where(it => it.endTime > Time.time - memoryWindow).ToList();
            // "Forget" (aka clamp) measurements to the memory window interval
            var first = results.First();
            first.startTime = Mathf.Max(first.startTime, Time.time - memoryWindow);
        }


        public bool HasSeenTarget(bool fastReact = false)
        {
            return fastReact ? canReactFast : canReact;
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
    }
}