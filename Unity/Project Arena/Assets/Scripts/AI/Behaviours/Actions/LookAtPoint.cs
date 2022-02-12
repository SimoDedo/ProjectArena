using System;
using System.Linq;
using AI.AI.Layer1;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class LookAtPoint : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathInfo;
        private Vector3 lookPoint;
        private SightController sightController;

        public override void OnAwake()
        {
            sightController = GetComponent<AIEntity>().SightController;
        }

        public override void OnStart()
        {
            lookPoint = pathInfo.Value.corners.Last();
        }


        public override TaskStatus OnUpdate()
        {
            sightController.LookAtPoint(lookPoint);
            return TaskStatus.Running;
        }
    }
}