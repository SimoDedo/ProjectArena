using System;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.NewBehaviours
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

        public override TaskStatus OnUpdate()
        {
            return navSystem.HasArrivedToDestination() ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}