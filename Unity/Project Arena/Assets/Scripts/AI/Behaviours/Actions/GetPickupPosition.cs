using System;
using AI.Layers.KnowledgeBase;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    /// <summary>
    /// Obtains the location of the pickup currently chosen by the PickupPlanner and saves it into the given
    /// ShaverVector3.
    /// </summary>
    [Serializable]
    public class GetPickupPosition : Action
    {
        [SerializeField] private SharedVector3 pickupPosition;
        private PickupPlanner pickupPlanner;

        public override void OnAwake()
        {
            pickupPlanner = GetComponent<AIEntity>().PickupPlanner;
        }

        public override TaskStatus OnUpdate()
        {
            var pickup = pickupPlanner.GetChosenPickup();
            pickupPosition.Value = pickup.transform.position;
            return TaskStatus.Success;
        }
    }
}