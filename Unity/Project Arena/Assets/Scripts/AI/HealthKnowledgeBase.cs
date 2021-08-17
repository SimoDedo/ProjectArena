using System.Collections.Generic;
using System.Linq;
using Accord.Statistics.Kernels;
using UnityEngine;

namespace AI
{
    public class HealthKnowledgeBase : MonoBehaviour
    {
        [SerializeField] private float maxSightRange;
        [SerializeField] private float fov;
        [SerializeField] private Transform head;
        public Dictionary<GameObject, float> estimatedActivationTime = new Dictionary<GameObject, float>();

        public void DetectPickups()
        {
            var healthSpawners = GameObject.FindGameObjectsWithTag("HealthPickup");
            foreach (var spawner in healthSpawners)
            {
                estimatedActivationTime.Add(spawner, Time.time);
            }
        }

        private void Update()
        {
            // if (estimatedActivationTime.Count == 0)
            // {
            //     Debug.LogError("I lost all data!");
            // }
            // var active = estimatedActivationTime.Values.Count(input => input <= Time.time);
            // Debug.Log("Pickup believed active are " + active);

            var keyList = new List<GameObject>(estimatedActivationTime.Keys);
            foreach (var obj in keyList)
            {
                var direction = obj.transform.position - head.position;
                if (Vector3.Angle(direction, head.forward) > fov) continue;
                if (Physics.Linecast(head.position, obj.transform.position)) continue;
                // Debug.Log("I Can see pickup " + obj.transform.position);
                var pickable = obj.GetComponent<MedkitPickable>();
                if (pickable.IsActive)
                {
                    // if (estimatedActivationTime[obj] > Time.time)
                    //     Debug.Log("Pickup previously considered inactive marked as active");
                    estimatedActivationTime[obj] = Time.time;
                }
                else
                {
                    // If we believed that the pickup was already active, then update the value to the average
                    // possible remaining time (aka cooldown / 2)
                    // Otherwise we already have an estimate on when the object will respawn, so do not make any new
                    // assumption
                    if (estimatedActivationTime[obj] < Time.time)
                    {
                        estimatedActivationTime[obj] = Time.time + pickable.Cooldown / 2;
                    }
                }
            }
        }

        public bool IsProbablyActive(GameObject medkit)
        {
            return estimatedActivationTime[medkit] <= Time.time;
        }

        public List<GameObject> GetProbablyActive()
        {
            var probablyActive =
                from data in estimatedActivationTime
                where data.Value < Time.time
                select data.Key;
            return probablyActive.ToList();
        }

        public void MarkConsumed(MedkitPickable medkit)
        {
            estimatedActivationTime[medkit.gameObject] = Time.time + medkit.Cooldown;
        }
    }
}