using System;
using System.Linq;
using AssemblyAI.AI.Layer2;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class ApplyPathToAgent : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathToApply;
        private Vector3 pathDestination;
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            navSystem = GetComponent<AIEntity>().NavigationSystem;
            pathDestination = pathToApply.Value.corners.Last();
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (navSystem.HasArrivedToDestination(pathDestination))
            {
                navSystem.CancelPath();
                return TaskStatus.Success;
            }
            navSystem.SetDestination(pathDestination);
            return TaskStatus.Running;
        }
    }
}