using System;
using System.Linq;
using AssemblyAI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class ApplyPathToAgent : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathToApply;
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            navSystem = GetComponent<NavigationSystem>();
            navSystem.SetPath(pathToApply.Value);
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            // Debug.DrawLine(transform.position, pathToApply.Value.corners.Last(), Color.magenta);
            return navSystem.HasArrivedToDestination() ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}