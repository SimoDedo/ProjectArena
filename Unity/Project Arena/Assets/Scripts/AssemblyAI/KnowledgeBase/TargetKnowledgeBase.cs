using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.KnowledgeBase
{
    public class TargetKnowledgeBase : MonoBehaviour
    {
        private class VisibilityData
        {
            public float startTime;
            public float endTime;
            public bool isVisibile;
        }

        private Entity target;
        private AISightSensor sensor;
        private float memoryWindow;
        
        /// <summary>
        /// Total time (in the memory window) that the enemy must be seen or not seen before declaring that
        /// we can detect it or have lost it.
        /// </summary>
        private float nonConsecutiveTimeBeforeReaction;
        
        private List<VisibilityData> results = new List<VisibilityData>();

        public void Prepare(AISightSensor sensor, Entity target, float memoryWindow,
            float nonConsecutiveTimeBeforeReaction)
        {
            this.target = target;
            this.sensor = sensor;
            this.memoryWindow = memoryWindow;
            this.nonConsecutiveTimeBeforeReaction = nonConsecutiveTimeBeforeReaction;
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
                if (last.isVisibile != result)
                    results.Add(new VisibilityData
                    {
                        isVisibile = result,
                        startTime = Time.time,
                        endTime = Time.time
                    });
            }
            else
            {
                results.Add(new VisibilityData
                {
                    isVisibile = result,
                    startTime = Time.time,
                    endTime = Time.time
                });
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


        public bool HasSeenTarget()
        {
            //Force updating since the enemy might have changed position since this component last update
            // or update might not have been called yet
            Update();

            return TestDetection();
        }

        private bool TestDetection()
        {
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

                if (t.isVisibile)
                {
                    totalTimeVisible += windowLenght;
                    if (totalTimeVisible > nonConsecutiveTimeBeforeReaction)
                        return true;
                }
                else
                {
                    totalTimeNotVisible += windowLenght;
                    if (totalTimeNotVisible > nonConsecutiveTimeBeforeReaction)
                        return false;
                }
            }
            return false;
        }

        public float GetLastKnownPositionTime()
        {
            var searchTimeEnd = Time.time;
            var result = results.FindLast(it => it.isVisibile && it.startTime < searchTimeEnd);
            // We got here not because we lost track of target, but for other reasons (e.g. got damage),
            // return current position of enemy
            return result?.startTime ?? Time.time;
        }
    }
}