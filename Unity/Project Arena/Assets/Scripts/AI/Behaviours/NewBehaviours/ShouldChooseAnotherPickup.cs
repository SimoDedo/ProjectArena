using System;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Controller;
using Entities.AI.Layer1.Sensors;
using UnityEngine;

namespace AI.Behaviours.NewBehaviours
{
    [Serializable]
    public class ShouldChooseAnotherPickup: Conditional
    {
        [SerializeField] private SharedSelectedPickupInfo pickupInfo;
        private AISightSensor sightSensor;
        public override void OnAwake()
        {
            sightSensor = GetComponent<AISightSensor>();
        }

        public override TaskStatus OnUpdate()
        {
            if (pickupInfo.Value.estimatedActivationTime > Time.time) return TaskStatus.Failure;
            if (!sightSensor.CanSeeObject(pickupInfo.Value.pickup.transform, Physics.AllLayers)) return TaskStatus.Failure;
            return pickupInfo.Value.pickup.IsActive ? TaskStatus.Failure : TaskStatus.Success;
        }
    }
}