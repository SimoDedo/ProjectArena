using System;
using AI.Behaviours.NewBehaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using Entities.AI.Layer2;
using UnityEngine;
using UnityEngine.AI;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.NewBehaviours
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

        public override TaskStatus OnUpdate()
        {
            return navSystem.HasArrivedToDestination() ? TaskStatus.Success : TaskStatus.Running;
        }
    }
}