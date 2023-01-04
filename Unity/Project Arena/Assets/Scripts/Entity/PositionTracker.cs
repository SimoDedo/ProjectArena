using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // TODO unless the time is far away in the past, you should loop from the end of the list (newest elems) towards
        // the beginning (old elements). 
        public Vector3 GetPositionAtTime(float time)
        {
            UpdateList();

            if (time < Time.time - MEMORY_WINDOW)
            {
                // I have no idea where the enemy was at that time, what should I return?
                // In which situation is this possible? It shouldn't be possible...
                throw new InvalidOperationException("No information known that far away in the past");
            }

            // Get interpolated position based on stored information
            var index = FindFirstIndex(info => info.time.CompareTo(time));

            if (index == tracked.Count - 1)
            {
                return tracked[index].position;
            }
            
            var (currentPos, currentTime) = tracked[index];
            var (nextPosition, nextTime) = tracked[index + 1];
            var fraction = (time - currentTime) / (nextTime - currentTime);
            var interpolatedPos = Vector3.Lerp(currentPos,nextPosition, fraction);

            return interpolatedPos;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FindFirstIndex(Func<PositionInfo, int> checker)
        {
            var begin = 0;
            var end = tracked.Count - 1;
            var biggestSmallerThan = -1;
            while (begin <= end)
            {
                var mid = (begin + end) / 2;

                var compare = checker.Invoke(tracked[mid]);
                if (compare < 0)
                {
                    biggestSmallerThan = mid;
                    begin = mid + 1;
                }
                else if (compare > 0)
                {
                    end = mid - 1;
                }
                else
                {
                    return mid;
                }
            }

            return biggestSmallerThan;
        }

        public Vector3 GetAverageVelocity(float intervalDuration)
        {
            var startTime = Time.time - Math.Min(MEMORY_WINDOW, intervalDuration);
            var endTime = Time.time;
            
            // Estimate velocity in the interval            
            const float WEIGHT = 60;

            var weighedVelocity = Vector3.zero;
            var trackedCount = tracked.Count;

            var next = tracked[0];
            for (var i = 0; i < trackedCount - 1; i++)
            {
                var current = next;
                next = tracked[i * 1];
                var intervalEndTime = next.time;
                if (intervalEndTime <= startTime) continue;
                
                var intervalStartTime = current.time;
                if (intervalStartTime > endTime) break;

                var velocity = SpeedBetween(current, next);

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
            
            var interpolatedPos = GetPositionAtTime(endTime);

            var trackedCount = tracked.Count;
            // Estimate velocity in the interval            
            const float WEIGHT = 60;

            var weighedVelocity = Vector3.zero;
            var (endPosition, intervalEndTime) = tracked[0];
            for (var i = 0; i < trackedCount - 1; i++)
            {
                var startPosition = endPosition;
                var intervalStartTime  = intervalEndTime;
                (endPosition, intervalEndTime) = tracked[i+1];
                if (intervalEndTime <= startTime) continue;
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