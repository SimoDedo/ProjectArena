using System;
using AI.AI.Layer2;
using AI.Behaviours.Variables;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;
using Action = BehaviorDesigner.Runtime.Tasks.Action;

namespace AI.Behaviours.Actions
{
    [Serializable]
    public class ApplyPathToAgent : Action
    {
        [SerializeField] private SharedSelectedPathInfo pathToApply;
        private NavigationSystem navSystem;

        public override void OnStart()
        {
            navSystem = GetComponent<AIEntity>().NavigationSystem;
            navSystem.SetPath(pathToApply.Value);
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