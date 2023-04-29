using System;
using AI.Layers.Planners;
using Bonsai;
using Bonsai.Core;
using UnityEngine;

namespace AI.BonsaiBehaviours.Actions
{
    /// <summary>
    /// Obtains the location of the pickup currently chosen by the PickupPlanner and saves it into the given
    /// ShaverVector3.
    /// </summary>
    [BonsaiNode("Tasks/")]
    public class GetPickupPosition : Task
    {
        private PickupPlanner pickupPlanner;

        public string pickupPositionKey;
        private Vector3 PickupPosition
        {
            set => Blackboard.Set(pickupPositionKey, value);
        }

        public override void OnStart()
        {
            pickupPlanner = Actor.GetComponent<AIEntity>().PickupPlanner;
        }

        public override Status Run()
        {
            var pickup = pickupPlanner.GetChosenPickup();
            PickupPosition = pickup.transform.position;
            return Status.Success;
        }
    }
}