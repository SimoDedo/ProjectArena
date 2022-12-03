using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

// TODO the circularQueue is dependent on the FPS of the game. Bad refresh rate will cause the positions
// to go even more in the past
// TODO Maybe remove this. Let the target knowledge base keep track of the last known enemy positions instead.
namespace Entity
{
    public class PositionTracker : MonoBehaviour
    {
        private class PositionInfo
        {
            public readonly Vector3 position;
            public readonly float time;

            public PositionInfo(Vector3 position, float time)
            {
                this.position = position;
                this.time = time;
            }

            public void Deconstruct(out Vector3 outPosition, out float outTime)
            {
                outPosition = position;
                outTime = time;
            }
        }
        private const float MEMORY_WINDOW = 10f;

        private readonly List<PositionInfo> tracked = new List<PositionInfo>();
        private Transform t;

        private void Start()
        {
            t = transform;
        }

        private void LateUpdate()
        {
            UpdateList();
        }

        public Vector3 GetPositionAtTime(float time)
        {
            UpdateList();

            if (time < Time.time - MEMORY_WINDOW)
            {
                // I have no idea where the enemy was at that time, what should I return?
                // In which situation is this possible? It shouldn't be possible...
                throw new InvalidOperationException("No information known that far away in the past");
            }

            var interpolatedPos = tracked[0].position;
            var trackedCount = tracked.Count;
            // Get interpolated position based on stored information
            for (var i = 0; i < trackedCount - 1; i++)
            {
                if (tracked[i + 1].time >= time)
                {
                    // The next element starts after the requested time, interpolate the position between the two
                    var fraction = (time - tracked[i].time) / (tracked[i + 1].time - tracked[i].time);
                    interpolatedPos = Vector3.Lerp(tracked[i].position,tracked[i + 1].position, fraction);
                    break;
                }
            }

            return interpolatedPos;
        }

        public Vector3 GetAverageVelocity(float intervalDuration)
        {
            var startTime = Time.time - Math.Min(MEMORY_WINDOW, intervalDuration);
            var endTime = Time.time;
            
            // Estimate velocity in the interval            
            const float WEIGHT = 60;

            var weighedVelocity = Vector3.zero;
            var trackedCount = tracked.Count;
            for (var i = 0; i < trackedCount - 1; i++)
            {
                var intervalEndTime = tracked[i + 1].time;
                if (intervalEndTime <= startTime) continue;
                var intervalStartTime = tracked[i].time;
                if (intervalStartTime > endTime) break;

                var velocity = SpeedBetween(tracked[i], tracked[i+1]);

                if (intervalStartTime < startTime)
                {
                    intervalStartTime = startTime;
                }

                if (intervalEndTime > endTime)
                {
                    intervalEndTime = endTime;
                }

                weighedVelocity += (Mathf.Pow(WEIGHT, intervalEndTime - endTime) -
                                    Mathf.Pow(WEIGHT, intervalStartTime - endTime)) * velocity;

            }

            var weightIntegral = 1 - Mathf.Pow(WEIGHT, startTime - endTime);
            if (weightIntegral == 0)
            {
                // Sometimes (e.g. startTime == endTime) integral area is zero.
                weightIntegral = 1;
            }

            return weighedVelocity / weightIntegral;
        }

        public Vector3 GetCurrentVelocity()
        {
            return SpeedBetween(tracked[^1], tracked[^2]);
        }

        private static Vector3 SpeedBetween(PositionInfo begin, PositionInfo end)
        {
            return (end.position - begin.position) / (end.time - begin.time);
        }

        /// TODO remove usages of this
        /// <summary>
        ///     Obtains the position and the velocity of this GameObject some time ago, according to delay.
        ///     The position is calculated by interpolating from the position samples saved, while the
        ///     velocity is obtained smoothing the velocity of the entity in every point in time saved
        ///     before delay, giving to each sample a weight that is proportional to
        ///     (1/2)^(MEMORY_WINDOW*MEMORY_WINDOW_END_WEIGHT/sample_delay).
        ///     In order to have a continuous sampling, the integral of such formula (stripped of constants)
        ///     is used.
        /// </summary>
        /// TODO
        /// <param name="startTime">Must come before endTime</param>
        /// <param name="endTime">The most recent time for which we want to know the position</param>
        /// <returns></returns>
        public Tuple<Vector3, Vector3> GetPositionAndVelocityForRange(float startTime, float endTime)
        {
            UpdateList();

            if (endTime < Time.time - MEMORY_WINDOW)
            {
                // I have no idea where the enemy was at that time, what should I return?
                // In which situation is this possible? It shouldn't be possible...
                throw new InvalidOperationException("No information known that far away in the past");
            }

            var interpolatedPos = tracked[0].position;
            // Get interpolated position based on stored information
            var trackedCount = tracked.Count;
            for (var i = 0; i < trackedCount - 1; i++)
            {
                if (tracked[i + 1].time >= endTime)
                {
                    // The next element starts after the requested time, interpolate the position between the two
                    var fraction = (endTime - tracked[i].time) / (tracked[i + 1].time - tracked[i].time);
                    interpolatedPos = Vector3.Lerp(tracked[i].position, tracked[i + 1].position, fraction);
                    break;
                }
            }

            // Estimate velocity in the interval            
            const float WEIGHT = 60;

            var weighedVelocity = Vector3.zero;

            for (var i = 0; i < trackedCount - 1; i++)
            {
                var (endPosition, intervalEndTime) = tracked[i + 1];
                if (intervalEndTime <= startTime) continue;
                var (startPosition, intervalStartTime) = tracked[i];
                if (intervalStartTime > endTime) break;

                var velocity = (endPosition - startPosition) / (intervalEndTime - intervalStartTime);

                if (intervalStartTime < startTime)
                {
                    intervalStartTime = startTime;
                }

                if (intervalEndTime > endTime)
                {
                    intervalEndTime = endTime;
                }

                weighedVelocity += (Mathf.Pow(WEIGHT, intervalEndTime - endTime) -
                                    Mathf.Pow(WEIGHT, intervalStartTime - endTime)) * velocity;

            }

            var weightIntegral = 1 - Mathf.Pow(WEIGHT, startTime - endTime);
            if (weightIntegral == 0)
            {
                // Sometimes (e.g. startTime == endTime) integral area is zero.
                weightIntegral = 1;
            }

            return new Tuple<Vector3, Vector3>(interpolatedPos, weighedVelocity / weightIntegral);
        }

        private void UpdateList()
        {
            if (tracked.Count != 0 && tracked.Last().time == Time.time)
                tracked.RemoveAt(tracked.Count - 1);

            tracked.Add(new PositionInfo(t.position, Time.time));
            // TODO check correctness
            var firstIndexToKeep = tracked.FindIndex(it => it.time > Time.time - MEMORY_WINDOW);
            if (firstIndexToKeep == -1)
            {
                tracked.Clear();
            }
            else
            {
                tracked.RemoveRange(0, firstIndexToKeep);
            }

            // positions = positions.Where(it => it.time > Time.time - MEMORY_WINDOW).ToList();
        }
    }
}