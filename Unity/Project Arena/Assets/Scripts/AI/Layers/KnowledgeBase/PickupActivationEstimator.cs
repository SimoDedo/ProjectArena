using System;
using System.Collections.Generic;
using System.Linq;
using AI.Layers.Memory;
using Pickables;
using UnityEngine;
using Utils;

namespace AI.Layers.KnowledgeBase
{
    public class PickupActivationEstimator
    {
        private readonly Dictionary<Pickable, float> estimatedActivationTime =
            new Dictionary<Pickable, float>(new MonoBehaviourEqualityComparer<Pickable>());

        private readonly AIEntity me;
        private PickupMemory memory;

        public PickupActivationEstimator(AIEntity me)
        {
            this.me = me;
        }

        /// <summary>
        /// Finish setting up the entity.
        /// </summary>
        public void Prepare()
        {
            memory = me.PickupMemory;
            var pickups = memory.GetPickups();
            foreach (var pickup in pickups)
            {
                estimatedActivationTime.Add(pickup, Time.time);
            }
        }

        /// <summary>
        /// Updates the knowledge base.
        /// </summary>
        public void Update()
        {
            // Get the list of pickups from the memory
            var pickupsInfo = memory.GetPickupsInfo();
            var keyList = new List<Pickable>(estimatedActivationTime.Keys);
            foreach (var pickable in keyList)
            {
                var pickupInfo = pickupsInfo[pickable];
                var lastTimeSeen = pickupInfo.lastTimeSeen;
                var lastTimeActive = pickupInfo.lastTimeSeenActive;

                if (lastTimeSeen > lastTimeActive)
                {
                    if (estimatedActivationTime[pickable] < Time.time)
                    {
                        // Object was not active last time we've seen it, estimate new activation time
                        estimatedActivationTime[pickable] = lastTimeSeen + pickable.Cooldown / 2;
                    }
                }
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                else if (lastTimeActive == lastTimeSeen)
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (lastTimeActive == pickupInfo.lastTimeCollected)
                    {
                        // Last time the object was picked up by us, so we now exactly the new activation time
                        estimatedActivationTime[pickable] = lastTimeActive + pickable.Cooldown;
                    }
                    else
                    {
                        // Last time we've seen the object it was active, no need to estimate.
                        estimatedActivationTime[pickable] = lastTimeActive;
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Get the list of pickups saved in the knowledge base.
        /// </summary>
        public List<Pickable> GetPickups()
        {
            return estimatedActivationTime.Keys.ToList();
        }

        /// <summary>
        /// Returns the estimated activation times for each power up handled by the knowledge base.
        /// </summary>
        public Dictionary<Pickable, float> GetPickupsEstimatedActivationTimes()
        {
            return estimatedActivationTime;
        }
    }
}