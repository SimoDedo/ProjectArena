using System.Collections.Generic;
using System.Linq;
using Entities.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.KnowledgeBase
{
    public class TargetKnowledgeBase : MonoBehaviour
    {
        private GameObject target;
        private AISightSensor sensor;
        public float memoryWindow;
        public float timeBeforeReaction;
        private Dictionary<float, bool> results = new Dictionary<float, bool>();

        public void SetParameters(AISightSensor sensor, GameObject target, float memoryWindow, float timeBeforeReaction)
        {
            this.target = target;
            this.sensor = sensor;
            this.memoryWindow = memoryWindow;
            this.timeBeforeReaction = timeBeforeReaction;
        }

        private void Update()
        {
            var result = sensor.CanSeeObject(target.transform, Physics.IgnoreRaycastLayer);
            AddToListAndCompact(result);
        }

        private void AddToListAndCompact(bool result)
        {
            results[Time.time] = result;
            results = results.Where(it => it.Key >= Time.time - memoryWindow).ToDictionary(
                pair => pair.Key, pair => pair.Value);
        }

        public bool CanReactToTarget()
        {
            //Force updating since the enemy might have changed position since this component last update
            // or update might not have been called yet
            Update();

            var keys = results.Keys.ToList();
            var totalTimeDetected = 0f;
            for (var i = 0; i < keys.Count - 1; i++)
            {
                if (results[keys[i]])
                {
                    totalTimeDetected += keys[i + 1] - keys[i];
                }
            }

            return totalTimeDetected > timeBeforeReaction;
        }

        public bool HasLostTarget()
        {
            //Force updating since the enemy might have changed position since this component last update
            // or update might not have been called yet
            // Update();

            // TODO find better logic
            return !CanReactToTarget();
        }
    }
}