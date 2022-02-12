using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// TODO the circularQueue is dependent on the FPS of the game. Bad refresh rate will cause the positions
// to go even more in the past
// TODO Maybe remove this. Let the target knowledge base keep track of the last known enemy positions instead.
namespace Entity
{
    public class PositionTracker : MonoBehaviour
    {
        private const float MEMORY_WINDOW = 0.5f;
        private const float MEMORY_WINDOW_END_WEIGHT = 10;

        private List<Tuple<Vector3, float>> positions = new List<Tuple<Vector3, float>>();
        private Transform t;

        private void Start()
        {
            t = transform;
        }

        private void LateUpdate()
        {
            UpdateList();
        }

        /// <summary>
        ///     Obtains the position and the velocity of this GameObject some time ago, according to delay.
        ///     The position is calculated by interpolating from the position samples saved, while the
        ///     velocity is obtained smoothing the velocity of the entity in every point in time saved
        ///     before delay, giving to each sample a weight that is proportional to
        ///     (1/2)^(MEMORY_WINDOW*MEMORY_WINDOW_END_WEIGHT/sample_delay).
        ///     In order to have a continuous sampling, the integral of such formula (stripped of constants)
        ///     is used.
        /// </summary>
        /// <param name="delay"></param>
        /// <returns></returns>
        public Tuple<Vector3, Vector3> GetPositionAndVelocityFromDelay(float delay)
        {
            UpdateList();
            var timeToSearch = Time.time - Mathf.Max(0, delay);
            // Step 1: find the next time instant saved after the delay
            var (afterPosition, afterTime) =
                positions.First(it => it.Item2 >= timeToSearch);
            // Step 2: find the previous time instant saved, if present, otherwise choose previous point again
            var (beforePosition, beforeTime) = positions.First().Item2 < timeToSearch
                ? positions.Last(it => it.Item2 < timeToSearch)
                : new Tuple<Vector3, float>(afterPosition, timeToSearch);

            // Step 3: interpolate the two
            Vector3 interpolatedPos;
            if (timeToSearch == afterTime)
                interpolatedPos = afterPosition;
            else
                interpolatedPos = Vector3.Lerp(beforePosition, afterPosition,
                    (timeToSearch - beforeTime) / (afterTime - beforeTime));


            // Select all element before beforeTime
            // Find index of beforeTime
            var beforeIndex = positions.FindIndex(it => it.Item2 == beforeTime);

            var smoothedVelocity = Vector3.zero;

            for (var i = 0; i <= beforeIndex; i++)
            {
                var beginDelay = timeToSearch - positions[i].Item2;
                var beginExponent = beginDelay * MEMORY_WINDOW_END_WEIGHT / MEMORY_WINDOW;
                var endDelay = timeToSearch - positions[i + 1].Item2;
                var endExponent = Math.Max(0, endDelay * MEMORY_WINDOW_END_WEIGHT / MEMORY_WINDOW);

                var velocity = (positions[i + 1].Item1 - positions[i].Item1) / (beginDelay - endDelay);

                var integral = Mathf.Pow(2, -endExponent) - Mathf.Pow(2, -beginExponent);
                smoothedVelocity += integral * velocity;
            }

            // var actualVelocity =
            //     (positions[positions.Count - 1].Item1 - positions[positions.Count - 2].Item1) /
            //     (positions[positions.Count - 1].Item2 - positions[positions.Count - 2].Item2);

            // Debug.Log("Difference between actual velocity and estimated velocity is "
            //           + (actualVelocity - smoothedVelocity).magnitude + "!");

            return new Tuple<Vector3, Vector3>(interpolatedPos, smoothedVelocity);
        }

        private void UpdateList()
        {
            if (positions.Count != 0 && positions.Last().Item2 == Time.time)
                positions.RemoveAt(positions.Count - 1);

            positions.Add(new Tuple<Vector3, float>(t.position, Time.time));
            positions = positions.Where(it => it.Item2 > Time.time - MEMORY_WINDOW).ToList();
        }
    }
}