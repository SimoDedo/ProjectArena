using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AssemblyAI.Behaviours.Actions
{
    [Serializable]
    public class MoveToDestination : Action
    {
        [SerializeField] private SharedVector3 destination;
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            navSystem = GetComponent<NavigationSystem>();
            var path = navSystem.CalculatePath(destination.Value);
            navSystem.SetPath(path);
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            Debug.DrawLine(transform.position, destination.Value, Color.green);
            return navSystem.HasArrivedToDestination() ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}