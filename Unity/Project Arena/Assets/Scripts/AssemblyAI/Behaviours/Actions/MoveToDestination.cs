using System;
using AssemblyAI.AI.Layer2;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
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
            navSystem = GetComponent<AIEntity>().NavigationSystem;
            navSystem.SetDestination(destination.Value);
        }

        public override void OnEnd()
        {
            navSystem.CancelPath();
        }

        public override TaskStatus OnUpdate()
        {
            if (navSystem.HasArrivedToDestination())
                return TaskStatus.Success;
            navSystem.MoveAlongPath();
            return TaskStatus.Running;
        }
    }
}